using MediatR;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Onboarding.Emails;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Onboarding.Tasks;

/// <summary>
/// Two entry points, distinguished by the task's AggregateType: a trial grant (aggregate = the
/// request, no payment involved) or a paid activation (aggregate = the payment, onboarding or
/// renewal). Fully idempotent by checking for an existing ClinicSubscription first.
/// </summary>
public sealed class ActivateSubscriptionTaskHandler(
    IClinicOnboardingRequestRepository requestRepository,
    ISubscriptionPaymentRepository paymentRepository,
    ISubscriptionPlanRepository planRepository,
    IClinicSubscriptionRepository subscriptionRepository,
    IWorkflowEnqueuer workflowEnqueuer,
    ISender sender)
    : IWorkflowTaskHandler
{
    public WorkflowTaskType TaskType => WorkflowTaskType.ActivateSubscription;

    public async Task HandleAsync(WorkflowTask task, CancellationToken ct)
    {
        if (task.AggregateType == nameof(ClinicOnboardingRequest))
            await HandleTrialGrantAsync(task.AggregateId, ct);
        else
            await HandlePaymentBasedAsync(task.AggregateId, ct);
    }

    private async Task HandleTrialGrantAsync(Guid requestId, CancellationToken ct)
    {
        var request = await requestRepository.GetByIdAsync(requestId, ct);
        if (request == null)
            throw new PermanentWorkflowException($"ClinicOnboardingRequest {requestId} not found.");

        if (!request.ProvisionedApplicationId.HasValue)
            throw new InvalidOperationException("Clinic has not been provisioned yet.");

        // Pay-as-you-go clinics have no subscription term — access is governed by wallet balance.
        // The guard middleware already bypasses the subscription check for PAYG clinics.
        if (request.ApprovalBillingMode == BillingMode.PayAsYouGo)
        {
            if (request.Status != ClinicOnboardingStatus.Active)
            {
                request.ActivatePayAsYouGo();
                await ApplyBillingIntentAsync(request, ct);
                await workflowEnqueuer.EnqueueAsync(
                    WorkflowTaskType.SendEmail, $"email:clinic-ready:{request.Id}", nameof(ClinicOnboardingRequest), request.Id,
                    OnboardingEmailTemplates.BuildPayload("ClinicReady", request.RequesterEmail, new { request.ClinicName }),
                    ct);
                await requestRepository.SaveChangesAsync(ct);
            }
            return;
        }

        var existing = await subscriptionRepository.GetByOnboardingRequestIdAsync(request.Id, ct);
        if (existing != null)
        {
            await FinishOnboardingActivationAsync(request, existing.Id, ct);
            return;
        }

        var plan = await planRepository.GetByCodeAsync(SubscriptionPlanCode.Trial, ct);
        if (plan == null || !plan.IsActive)
            throw new PermanentWorkflowException("The Trial plan is not configured.");

        var subscription = ClinicSubscription.Create(
            request.ProvisionedApplicationId.Value, plan, DateTime.UtcNow,
            request.ClinicName, request.RequesterEmail, request.RequesterName,
            purchasedByUserId: request.RequestedByUserId,
            onboardingRequestId: request.Id);

        await subscriptionRepository.AddAsync(subscription, ct);
        await FinishOnboardingActivationAsync(request, subscription.Id, ct);
    }

    private async Task HandlePaymentBasedAsync(Guid paymentId, CancellationToken ct)
    {
        var payment = await paymentRepository.GetByIdAsync(paymentId, ct);
        if (payment == null)
            throw new PermanentWorkflowException($"SubscriptionPayment {paymentId} not found.");

        if (payment.Status != SubscriptionPaymentStatus.Paid)
            throw new InvalidOperationException("Payment is not marked Paid yet.");

        var existingByPayment = await subscriptionRepository.GetByPaymentIdAsync(payment.Id, ct);
        if (existingByPayment != null)
        {
            payment.AttachSubscription(existingByPayment.Id);
            await FinishAfterActivationAsync(payment, existingByPayment.Id, existingByPayment.EndDate, ct);
            return;
        }

        // A plan is mandatory here. Activation only ever runs for the three plan-carrying purposes;
        // a wallet top-up or a registration fee is branched away by ConfirmPaymentTaskHandler, so a
        // null plan on this path is a data defect rather than a normal case.
        if (payment.SubscriptionPlanId is not { } planId)
            throw new PermanentWorkflowException(
                $"SubscriptionPayment {payment.Id} ({payment.Purpose}) carries no plan and cannot activate a subscription.");

        var plan = await planRepository.GetByIdAsync(planId, ct);
        if (plan == null)
            throw new PermanentWorkflowException($"SubscriptionPlan {planId} not found.");

        Guid applicationId;
        string billingEmail;
        string billingName;
        Guid? purchasedByUserId;

        ClinicOnboardingRequest? onboardingRequest = null;

        if (payment.Purpose == PaymentPurpose.Onboarding && payment.OnboardingRequestId.HasValue)
        {
            onboardingRequest = await requestRepository.GetByIdAsync(payment.OnboardingRequestId.Value, ct);
            if (onboardingRequest == null)
                throw new PermanentWorkflowException($"ClinicOnboardingRequest {payment.OnboardingRequestId} not found.");
            if (!onboardingRequest.ProvisionedApplicationId.HasValue)
                throw new InvalidOperationException("Clinic has not been provisioned yet.");

            applicationId = onboardingRequest.ProvisionedApplicationId.Value;
            billingEmail = onboardingRequest.RequesterEmail;
            billingName = onboardingRequest.RequesterName;
            purchasedByUserId = onboardingRequest.RequestedByUserId;
        }
        else
        {
            if (!payment.ApplicationId.HasValue)
                throw new PermanentWorkflowException($"Renewal payment {payment.Id} has no ApplicationId.");

            applicationId = payment.ApplicationId.Value;
            billingEmail = payment.PayerEmail;
            billingName = payment.PayerName;
            purchasedByUserId = payment.InitiatedByUserId;
        }

        var utcNow = DateTime.UtcNow;

        // Start exactly where the clinic's existing coverage ends — counting terms already bought
        // but not started, so buying twice in a row chains correctly instead of overlapping. There
        // is deliberately no "+1 day": that would lock the clinic out for a day between terms.
        var coverageEnd = await subscriptionRepository.GetCoverageEndAsync(applicationId, utcNow, ct);
        var startDate = coverageEnd ?? utcNow;

        // The clinic's display name is snapshotted (deliberately no Auth call from the background
        // worker): the onboarding request's own name, else the one recorded on the payment when
        // the link was made, else the previous subscription's.
        var latest = await subscriptionRepository.GetLatestForClinicAsync(applicationId, ct);
        var clinicName = onboardingRequest?.ClinicName ?? payment.ClinicName ?? latest?.ClinicName ?? plan.Name;

        var subscription = ClinicSubscription.Create(
            applicationId, plan, startDate, clinicName, billingEmail, billingName,
            purchasedByUserId: purchasedByUserId, subscriptionPaymentId: payment.Id,
            pricePaidOverride: payment.Amount);

        await subscriptionRepository.AddAsync(subscription, ct);
        payment.AttachSubscription(subscription.Id);

        if (onboardingRequest != null)
        {
            await FinishOnboardingActivationAsync(onboardingRequest, subscription.Id, ct);
        }
        else
        {
            await workflowEnqueuer.EnqueueAsync(
                WorkflowTaskType.SendEmail, $"email:renewal-activated:{payment.Id}", nameof(SubscriptionPayment), payment.Id,
                OnboardingEmailTemplates.BuildPayload("RenewalActivated", payment.PayerEmail, new
                {
                    ClinicName = clinicName,
                    StartDate = subscription.StartDate.ToString("dd MMM yyyy"),
                    EndDate = subscription.EndDate.ToString("dd MMM yyyy")
                }),
                ct);

            await paymentRepository.SaveChangesAsync(ct);
        }
    }

    private async Task FinishAfterActivationAsync(SubscriptionPayment payment, Guid subscriptionId, DateTime endDate, CancellationToken ct)
    {
        if (payment.Purpose == PaymentPurpose.Onboarding && payment.OnboardingRequestId.HasValue)
        {
            var request = await requestRepository.GetByIdAsync(payment.OnboardingRequestId.Value, ct);
            if (request != null)
            {
                await FinishOnboardingActivationAsync(request, subscriptionId, ct);
                return;
            }
        }

        await paymentRepository.SaveChangesAsync(ct);
    }

    private async Task FinishOnboardingActivationAsync(ClinicOnboardingRequest request, Guid subscriptionId, CancellationToken ct)
    {
        if (request.Status != ClinicOnboardingStatus.Active)
        {
            request.Activate(subscriptionId);
            await ApplyBillingIntentAsync(request, ct);
            await workflowEnqueuer.EnqueueAsync(
                WorkflowTaskType.SendEmail, $"email:clinic-ready:{request.Id}", nameof(ClinicOnboardingRequest), request.Id,
                OnboardingEmailTemplates.BuildPayload("ClinicReady", request.RequesterEmail, new { request.ClinicName }),
                ct);
        }

        await requestRepository.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Applies any billing setup that was captured at approval time. Called after the clinic is
    /// activated; uses the provisioned ApplicationId which does not exist until that point. All
    /// commands are idempotent — safe to replay if the workflow task is retried.
    /// </summary>
    private async Task ApplyBillingIntentAsync(ClinicOnboardingRequest request, CancellationToken ct)
    {
        if (!request.ProvisionedApplicationId.HasValue) return;

        var appId = request.ProvisionedApplicationId.Value;
        var adminEmail = request.ReviewedByAdminEmail ?? "system@tenantcore.internal";
        var adminId = request.ReviewedByAdminId ?? Guid.Empty;
        const string reason = "Set at clinic activation";

        // A PAYG clinic with a registration fee must pay the fee before the wallet unlocks.
        // The initial credit is stored as PendingWalletCreditOnFeePayment and released by
        // RecordRegistrationFeeTaskHandler once the payment is confirmed.
        var deferCreditUntilFeePayment =
            request.ApprovalBillingMode == BillingMode.PayAsYouGo
            && request.ApprovalRegistrationFee is { } regFee && regFee > 0m
            && request.ApprovalInitialWalletCredit is { } c && c > 0m;

        // Billing mode, registration fee and free test days — and, when deferral applies, the
        // pending wallet credit — all go in one command so they land in a single SaveChanges.
        if (request.ApprovalBillingMode.HasValue
            || request.ApprovalRegistrationFee.HasValue
            || request.ApprovalFreeTestDays.HasValue)
        {
            await sender.Send(new SetClinicBillingCommand(
                appId,
                request.ApprovalBillingMode ?? BillingMode.Subscription,
                ApplyNow: true,
                request.ApprovalRegistrationFee,
                request.ApprovalFreeTestDays,
                reason,
                adminId,
                adminEmail,
                InitialWalletCreditOnFeePayment: deferCreditUntilFeePayment
                    ? request.ApprovalInitialWalletCredit
                    : null), ct);
        }

        // Usage rates
        var rates = new List<UsageRateSetting>();
        if (request.ApprovalOpdRate.HasValue)
            rates.Add(new UsageRateSetting(BillingItem.OpdRegistration, request.ApprovalOpdRate.Value));
        if (request.ApprovalIpdAdmissionRate.HasValue)
            rates.Add(new UsageRateSetting(BillingItem.IpdAdmission, request.ApprovalIpdAdmissionRate.Value));
        if (request.ApprovalOtSurgeryRate.HasValue)
            rates.Add(new UsageRateSetting(BillingItem.OtSurgery, request.ApprovalOtSurgeryRate.Value));
        if (request.ApprovalIpdDischargeRate.HasValue)
            rates.Add(new UsageRateSetting(BillingItem.IpdDischarge, request.ApprovalIpdDischargeRate.Value));

        if (rates.Count > 0)
        {
            await sender.Send(new SetClinicUsageRatesCommand(
                appId, rates, reason, adminId, adminEmail), ct);
        }

        // Initial wallet credit — only applied immediately when the clinic has no fee to pay first.
        // Deferred clinics get the credit in RecordRegistrationFeeTaskHandler after fee confirmation.
        if (!deferCreditUntilFeePayment && request.ApprovalInitialWalletCredit is { } credit && credit > 0m)
        {
            await sender.Send(new CreditClinicWalletCommand(
                appId, credit,
                "Initial wallet credit at clinic activation",
                request.Id,  // idempotency key
                adminId, adminEmail), ct);
        }
    }
}
