using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface ISubscriptionPaymentRepository : IRepository<SubscriptionPayment>
{
    Task<SubscriptionPayment?> GetByGatewayLinkIdAsync(string gatewayPaymentLinkId, CancellationToken ct = default);

    /// <summary>Payments still LinkCreated and older than the given age — the reconciliation sweep's input.</summary>
    Task<IReadOnlyList<SubscriptionPayment>> GetDueForReconciliationAsync(DateTime olderThan, int batchSize, CancellationToken ct = default);

    /// <summary>AsNoTracking, newest first, capped at 100.</summary>
    Task<IReadOnlyList<SubscriptionPayment>> GetForClinicAsync(Guid applicationId, CancellationToken ct = default);

    /// <summary>
    /// The clinic's currently payable link (Pending/LinkCreated) for either a self-serve renewal or
    /// an admin-assigned plan. At most one is open at a time, so a new one supersedes it.
    /// </summary>
    Task<SubscriptionPayment?> GetOpenLinkForClinicAsync(Guid applicationId, CancellationToken ct = default);

    /// <summary>Tracked. Null when the payment does not exist or belongs to another clinic (treated as not found).</summary>
    Task<SubscriptionPayment?> GetByIdForClinicAsync(Guid id, Guid applicationId, CancellationToken ct = default);

    /// <summary>
    /// The clinic's most recent registration fee payment (Open or Expired), no-tracking.
    /// Returns null when no registration fee payment has ever been created for this clinic.
    /// Used to show the clinic whether a link already exists and whether it is still payable.
    /// </summary>
    Task<SubscriptionPayment?> GetLatestRegistrationFeePaymentAsync(Guid applicationId, CancellationToken ct = default);
}
