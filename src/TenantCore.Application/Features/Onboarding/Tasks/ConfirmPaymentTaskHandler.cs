using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Onboarding.Tasks;

/// <summary>
/// Shared by the webhook and the reconciliation sweep. Always re-fetches the link from Razorpay
/// rather than trusting a webhook payload, and confirms the amount matches this payment's own
/// approved amount (not the plan's list price) before marking it Paid. Fully idempotent — safe
/// to run any number of times, in any order relative to the webhook and reconciliation.
/// </summary>
public sealed class ConfirmPaymentTaskHandler(
    ISubscriptionPaymentRepository paymentRepository,
    IClinicOnboardingRequestRepository requestRepository,
    IPaymentGateway paymentGateway,
    IWorkflowEnqueuer workflowEnqueuer)
    : IWorkflowTaskHandler
{
    public WorkflowTaskType TaskType => WorkflowTaskType.ConfirmPayment;

    public async Task HandleAsync(WorkflowTask task, CancellationToken ct)
    {
        var payment = await paymentRepository.GetByIdAsync(task.AggregateId, ct);
        if (payment == null)
            throw new PermanentWorkflowException($"SubscriptionPayment {task.AggregateId} not found.");

        if (payment.Status == SubscriptionPaymentStatus.Paid)
        {
            // Already confirmed — but make sure the downstream step was actually enqueued, in
            // case an earlier attempt crashed after MarkPaid but before reaching that point.
            await EnqueueDownstreamAsync(payment, ct, save: true);
            return;
        }

        if (payment.Status is SubscriptionPaymentStatus.Superseded or SubscriptionPaymentStatus.Cancelled or SubscriptionPaymentStatus.Expired)
        {
            if (payment.OnboardingRequestId.HasValue)
            {
                var closedRequest = await requestRepository.GetByIdAsync(payment.OnboardingRequestId.Value, ct);
                closedRequest?.FlagAttention($"Payment {payment.Id} was confirmed after it had already been marked {payment.Status} — check whether a refund is owed.");
                await requestRepository.SaveChangesAsync(ct);
            }
            return;
        }

        if (string.IsNullOrEmpty(payment.GatewayPaymentLinkId))
            throw new WaitingWorkflowException("Payment has no gateway link yet — waiting for link creation.");

        var (result, link) = await paymentGateway.GetPaymentLinkByIdAsync(payment.GatewayPaymentLinkId, ct);
        if (!result.Success || link == null)
            throw new InvalidOperationException(result.ErrorMessage ?? "Could not fetch the payment link from Razorpay.");

        if (link.Status != "paid" || string.IsNullOrEmpty(link.PaymentId))
            throw new WaitingWorkflowException($"Payment link status is '{link.Status}' — waiting for the clinic to pay.");

        if (link.AmountPaid.HasValue && link.AmountPaid.Value != payment.Amount)
        {
            if (payment.OnboardingRequestId.HasValue)
            {
                var request = await requestRepository.GetByIdAsync(payment.OnboardingRequestId.Value, ct);
                request?.FlagAttention($"Amount mismatch on payment {payment.Id}: expected {payment.Amount}, Razorpay reports {link.AmountPaid}.");
                await requestRepository.SaveChangesAsync(ct);
            }

            throw new PermanentWorkflowException($"Amount mismatch on payment {payment.Id}: expected {payment.Amount}, got {link.AmountPaid}.");
        }

        payment.MarkPaid(link.PaymentId, link.Method);
        await EnqueueDownstreamAsync(payment, ct, save: false);
        await paymentRepository.SaveChangesAsync(ct);
    }

    private async Task EnqueueDownstreamAsync(SubscriptionPayment payment, CancellationToken ct, bool save)
    {
        if (payment.Purpose == PaymentPurpose.Onboarding && payment.OnboardingRequestId.HasValue)
        {
            var request = await requestRepository.GetByIdAsync(payment.OnboardingRequestId.Value, ct);
            if (request == null)
                return;

            if (request.Status is ClinicOnboardingStatus.Rejected or ClinicOnboardingStatus.Cancelled)
            {
                request.FlagAttention($"Payment {payment.Id} was confirmed for a {request.Status} request — refund required.");
                if (save) await requestRepository.SaveChangesAsync(ct);
                return;
            }

            if (request.Status == ClinicOnboardingStatus.AwaitingPayment)
                request.MarkPaymentReceived();

            if (request.Status is ClinicOnboardingStatus.PaymentReceived or ClinicOnboardingStatus.Provisioning)
            {
                request.MarkProvisioning();
                await workflowEnqueuer.EnqueueAsync(
                    WorkflowTaskType.ProvisionClinic, $"provision:{request.Id}", nameof(ClinicOnboardingRequest), request.Id, ct: ct);
            }
            // else: already Active — nothing further to do.

            if (save) await requestRepository.SaveChangesAsync(ct);
        }
        else if (payment.IsClinicLink)
        {
            // Renewals and admin-assigned links both activate the same way — the term is queued
            // after the clinic's existing coverage by ActivateSubscriptionTaskHandler.
            await workflowEnqueuer.EnqueueAsync(
                WorkflowTaskType.ActivateSubscription, $"activate-subscription:{payment.Id}", nameof(SubscriptionPayment), payment.Id, ct: ct);

            if (save) await paymentRepository.SaveChangesAsync(ct);
        }
    }
}
