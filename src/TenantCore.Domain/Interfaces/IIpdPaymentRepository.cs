using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IIpdPaymentRepository : IClinicRepository<IpdPayment>
{
    Task<IpdPayment?> GetByRegistrationAsync(Guid ipdRegistrationId, Guid applicationId, CancellationToken ct = default);
    Task<IEnumerable<IpdPayment>> GetByRegistrationIdsAsync(IEnumerable<Guid> ipdRegistrationIds, Guid applicationId, CancellationToken ct = default);

    // Settled/refunded in [fromUtc, toUtc) — the revenue report reads both from this one set.
    Task<IEnumerable<IpdPayment>> GetSettledOrRefundedByDateRangeAsync(DateTime fromUtc, DateTime toUtc, Guid applicationId, CancellationToken ct = default);
}
