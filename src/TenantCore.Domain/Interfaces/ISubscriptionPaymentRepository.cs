using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface ISubscriptionPaymentRepository : IRepository<SubscriptionPayment>
{
    Task<SubscriptionPayment?> GetByGatewayLinkIdAsync(string gatewayPaymentLinkId, CancellationToken ct = default);

    /// <summary>Payments still LinkCreated and older than the given age — the reconciliation sweep's input.</summary>
    Task<IReadOnlyList<SubscriptionPayment>> GetDueForReconciliationAsync(DateTime olderThan, int batchSize, CancellationToken ct = default);

    /// <summary>AsNoTracking, newest first, capped at 100.</summary>
    Task<IReadOnlyList<SubscriptionPayment>> GetForClinicAsync(Guid applicationId, CancellationToken ct = default);

    /// <summary>The clinic's currently payable renewal link (Pending/LinkCreated), if any — avoids creating a second one.</summary>
    Task<SubscriptionPayment?> GetOpenRenewalForClinicAsync(Guid applicationId, CancellationToken ct = default);
}
