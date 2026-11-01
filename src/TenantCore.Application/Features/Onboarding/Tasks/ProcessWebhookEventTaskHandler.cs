using System.Text.Json;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Onboarding.Emails;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Onboarding.Tasks;

public sealed class ProcessWebhookEventTaskHandler(
    IPaymentWebhookEventRepository webhookEventRepository,
    ISubscriptionPaymentRepository paymentRepository,
    IClinicOnboardingRequestRepository requestRepository,
    IWorkflowEnqueuer workflowEnqueuer)
    : IWorkflowTaskHandler
{
    public WorkflowTaskType TaskType => WorkflowTaskType.ProcessWebhookEvent;

    public async Task HandleAsync(WorkflowTask task, CancellationToken ct)
    {
        var webhookEvent = await webhookEventRepository.GetByIdAsync(task.AggregateId, ct);
        if (webhookEvent == null)
            throw new PermanentWorkflowException($"PaymentWebhookEvent {task.AggregateId} not found.");

        if (webhookEvent.ProcessedAt.HasValue)
            return; // idempotent

        using var doc = JsonDocument.Parse(webhookEvent.PayloadJson);
        var referenceId = ExtractReferenceId(doc.RootElement);
        var paymentId = referenceId != null && Guid.TryParse(referenceId, out var pid) ? pid : (Guid?)null;

        switch (webhookEvent.EventType)
        {
            case "payment_link.paid":
                if (paymentId.HasValue)
                {
                    await workflowEnqueuer.EnqueueAsync(
                        WorkflowTaskType.ConfirmPayment, $"confirm-payment:{paymentId.Value}", nameof(SubscriptionPayment), paymentId.Value, ct: ct);
                }
                break;

            case "payment_link.expired":
                if (paymentId.HasValue)
                    await HandleExpiredOrCancelledAsync(paymentId.Value, expired: true, ct);
                break;

            case "payment_link.cancelled":
                if (paymentId.HasValue)
                    await HandleExpiredOrCancelledAsync(paymentId.Value, expired: false, ct);
                break;

            default:
                // Unrecognised event — mark processed and move on so Razorpay stops retrying it.
                break;
        }

        webhookEvent.MarkProcessed();
        await webhookEventRepository.SaveChangesAsync(ct);
    }

    private async Task HandleExpiredOrCancelledAsync(Guid paymentId, bool expired, CancellationToken ct)
    {
        var payment = await paymentRepository.GetByIdAsync(paymentId, ct);
        if (payment == null)
            return;

        if (expired) payment.MarkExpired(); else payment.MarkCancelled();

        if (expired && payment.Purpose == PaymentPurpose.Onboarding && payment.OnboardingRequestId.HasValue)
        {
            var request = await requestRepository.GetByIdAsync(payment.OnboardingRequestId.Value, ct);
            if (request != null)
            {
                request.MarkLinkExpired();
                await workflowEnqueuer.EnqueueAsync(
                    WorkflowTaskType.SendEmail, $"email:link-expired:{payment.Id}", nameof(ClinicOnboardingRequest), request.Id,
                    OnboardingEmailTemplates.BuildPayload("LinkExpired", request.RequesterEmail, new { request.ClinicName }),
                    ct);
            }
        }

        // Saved once, together with webhookEvent.MarkProcessed(), by the caller — HandleAsync.
    }

    /// <summary>Razorpay webhook envelope: {"event": "...", "payload": {"payment_link": {"entity": {"reference_id": "..."}}}}</summary>
    private static string? ExtractReferenceId(JsonElement root)
    {
        if (root.TryGetProperty("payload", out var payload) &&
            payload.TryGetProperty("payment_link", out var paymentLink) &&
            paymentLink.TryGetProperty("entity", out var entity) &&
            entity.TryGetProperty("reference_id", out var refId))
        {
            return refId.GetString();
        }
        return null;
    }
}
