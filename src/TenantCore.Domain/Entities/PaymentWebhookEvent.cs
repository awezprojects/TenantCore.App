using TenantCore.Domain.Common;

namespace TenantCore.Domain.Entities;

/// <summary>
/// Webhook inbox — the endpoint only verifies the signature and stores the raw event here
/// (unique on EventId, so a duplicate delivery is ignored at insert). Processing happens in a
/// retryable WorkflowTask, so a processing failure never loses the event. Not tenant-scoped.
/// </summary>
public class PaymentWebhookEvent : BaseEntity
{
    public string Gateway { get; private set; } = string.Empty;

    /// <summary>Razorpay's x-razorpay-event-id — unique.</summary>
    public string EventId { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = string.Empty;
    public DateTime ReceivedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    private PaymentWebhookEvent() { }

    public static PaymentWebhookEvent Create(string gateway, string eventId, string eventType, string payloadJson) => new()
    {
        Id = Guid.NewGuid(),
        Gateway = gateway,
        EventId = eventId,
        EventType = eventType,
        PayloadJson = payloadJson,
        ReceivedAt = DateTime.UtcNow,
        CreatedAt = DateTime.UtcNow
    };

    public void MarkProcessed() => ProcessedAt = DateTime.UtcNow;
}
