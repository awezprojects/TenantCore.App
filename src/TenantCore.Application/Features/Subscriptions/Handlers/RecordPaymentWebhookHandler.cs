using System.Text.Json;
using MediatR;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Subscriptions.Commands;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.Subscriptions.Handlers;

public sealed class RecordPaymentWebhookHandler(
    IPaymentWebhookEventRepository webhookEventRepository,
    IPaymentGateway paymentGateway,
    IWorkflowEnqueuer workflowEnqueuer)
    : IRequestHandler<RecordPaymentWebhookCommand>
{
    public async Task Handle(RecordPaymentWebhookCommand command, CancellationToken ct)
    {
        if (!paymentGateway.VerifyWebhookSignature(command.RawBody, command.Signature))
            throw new UnauthorizedAccessException("Invalid webhook signature.");

        var eventType = ExtractEventType(command.RawBody);
        var webhookEvent = PaymentWebhookEvent.Create("Razorpay", command.EventId, eventType, command.RawBody);

        var added = await webhookEventRepository.TryAddAsync(webhookEvent, ct);
        if (!added)
            return; // duplicate delivery — already recorded, nothing more to do

        await workflowEnqueuer.EnqueueAsync(
            WorkflowTaskType.ProcessWebhookEvent, $"process-webhook:{webhookEvent.Id}", nameof(PaymentWebhookEvent), webhookEvent.Id, ct: ct);

        await webhookEventRepository.SaveChangesAsync(ct);
    }

    private static string ExtractEventType(string rawBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            return doc.RootElement.TryGetProperty("event", out var e) ? e.GetString() ?? "unknown" : "unknown";
        }
        catch (JsonException)
        {
            return "unknown";
        }
    }
}
