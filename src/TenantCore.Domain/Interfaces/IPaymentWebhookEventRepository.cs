using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IPaymentWebhookEventRepository : IRepository<PaymentWebhookEvent>
{
    /// <summary>Adds the event and saves. Returns false (adds nothing) when EventId already exists — the caller then knows to skip re-enqueueing its processing task.</summary>
    Task<bool> TryAddAsync(PaymentWebhookEvent webhookEvent, CancellationToken ct = default);
}
