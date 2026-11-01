using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Onboarding.Tasks;

/// <summary>
/// Cancels a payment's link at Razorpay. Two callers, two outcomes on success:
/// rejection — the payment is simply marked Cancelled; change-amount — ReplacedByPaymentId is
/// set, so success instead supersedes this payment and creates the replacement's link. If the
/// link turns out to already be paid, that payment is kept (never silently discarded) and the
/// replacement, if any, is cancelled without ever getting a link — the doctor can never pay twice.
/// </summary>
public sealed class CancelPaymentLinkTaskHandler(
    ISubscriptionPaymentRepository paymentRepository,
    IClinicOnboardingRequestRepository requestRepository,
    IPaymentGateway paymentGateway,
    IWorkflowEnqueuer workflowEnqueuer)
    : IWorkflowTaskHandler
{
    public WorkflowTaskType TaskType => WorkflowTaskType.CancelPaymentLink;

    public async Task HandleAsync(WorkflowTask task, CancellationToken ct)
    {
        var payment = await paymentRepository.GetByIdAsync(task.AggregateId, ct);
        if (payment == null)
            throw new PermanentWorkflowException($"SubscriptionPayment {task.AggregateId} not found.");

        if (payment.Status is SubscriptionPaymentStatus.Superseded or SubscriptionPaymentStatus.Cancelled
            or SubscriptionPaymentStatus.Expired or SubscriptionPaymentStatus.Paid)
            return; // already terminal — idempotent no-op

        if (string.IsNullOrEmpty(payment.GatewayPaymentLinkId))
        {
            // No link was ever created at Razorpay — nothing to cancel there.
            await CompleteAfterCancelAsync(payment, ct);
            return;
        }

        var cancelResult = await paymentGateway.CancelPaymentLinkAsync(payment.GatewayPaymentLinkId, ct);

        if (cancelResult.Success)
        {
            await CompleteAfterCancelAsync(payment, ct);
            return;
        }

        if (cancelResult.IsTransient)
            throw new InvalidOperationException(cancelResult.ErrorMessage ?? "Razorpay cancel payment link failed.");

        // A permanent failure here usually means the link was already paid — confirm directly
        // rather than trusting the error text.
        var (linkResult, link) = await paymentGateway.GetPaymentLinkByIdAsync(payment.GatewayPaymentLinkId, ct);
        if (linkResult.Success && link is { Status: "paid" })
        {
            await HandleAlreadyPaidAsync(payment, ct);
            return;
        }

        throw new PermanentWorkflowException(cancelResult.ErrorMessage ?? "Razorpay cancel payment link failed permanently.");
    }

    private async Task CompleteAfterCancelAsync(SubscriptionPayment payment, CancellationToken ct)
    {
        if (payment.ReplacedByPaymentId.HasValue)
        {
            payment.MarkSuperseded(payment.ReplacedByPaymentId.Value);
            await workflowEnqueuer.EnqueueAsync(
                WorkflowTaskType.CreatePaymentLink, $"create-link:{payment.ReplacedByPaymentId.Value}",
                nameof(SubscriptionPayment), payment.ReplacedByPaymentId.Value, ct: ct);
        }
        else
        {
            payment.MarkCancelled();
        }

        await paymentRepository.SaveChangesAsync(ct);
    }

    private async Task HandleAlreadyPaidAsync(SubscriptionPayment payment, CancellationToken ct)
    {
        if (payment.ReplacedByPaymentId.HasValue)
        {
            // Change-amount race: the old link got paid right as we tried to replace it.
            // Keep the payment that actually happened; cancel the never-linked replacement.
            var replacement = await paymentRepository.GetByIdAsync(payment.ReplacedByPaymentId.Value, ct);
            replacement?.MarkCancelled();

            await workflowEnqueuer.EnqueueAsync(
                WorkflowTaskType.ConfirmPayment, $"confirm-payment:{payment.Id}", nameof(SubscriptionPayment), payment.Id, ct: ct);

            await paymentRepository.SaveChangesAsync(ct);
            return;
        }

        // Rejection case — money arrived for a request that's already rejected/cancelled.
        // Never auto-provision from here; flag for a manual refund instead.
        if (payment.OnboardingRequestId.HasValue)
        {
            var request = await requestRepository.GetByIdAsync(payment.OnboardingRequestId.Value, ct);
            request?.FlagAttention($"Payment {payment.Id} was paid after its request was closed — refund required.");
            await requestRepository.SaveChangesAsync(ct);
            return;
        }

        // A clinic link (renewal or admin-assigned) that was paid just as it was being cancelled.
        // There is no request to flag and no replacement to fall back on, so confirm it: money
        // that arrived always ends in an activated term rather than being silently dropped.
        if (payment.IsClinicLink)
        {
            await workflowEnqueuer.EnqueueAsync(
                WorkflowTaskType.ConfirmPayment, $"confirm-payment:{payment.Id}", nameof(SubscriptionPayment), payment.Id, ct: ct);

            await paymentRepository.SaveChangesAsync(ct);
        }
    }
}
