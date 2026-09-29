using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Onboarding.Emails;
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
    IWorkflowEnqueuer workflowEnqueuer)
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

        var plan = await planRepository.GetByIdAsync(payment.SubscriptionPlanId, ct);
        if (plan == null)
            throw new PermanentWorkflowException($"SubscriptionPlan {payment.SubscriptionPlanId} not found.");

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

        var latest = await subscriptionRepository.GetLatestForClinicAsync(applicationId, ct);
        var utcNow = DateTime.UtcNow;
        var startDate = latest is not null && latest.IsCurrentlyActive(utcNow) ? latest.EndDate.AddDays(1) : utcNow;

        // The clinic's display name is snapshotted from the previous subscription row where one
        // exists (deliberately no Auth call from the background worker) — a renewal always has
        // one, since the trial or first paid term already created it. Onboarding always has its
        // own request's ClinicName instead.
        var clinicName = onboardingRequest?.ClinicName ?? latest?.ClinicName ?? plan.Name;

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
            await workflowEnqueuer.EnqueueAsync(
                WorkflowTaskType.SendEmail, $"email:clinic-ready:{request.Id}", nameof(ClinicOnboardingRequest), request.Id,
                OnboardingEmailTemplates.BuildPayload("ClinicReady", request.RequesterEmail, new { request.ClinicName }),
                ct);
        }

        await requestRepository.SaveChangesAsync(ct);
    }
}
