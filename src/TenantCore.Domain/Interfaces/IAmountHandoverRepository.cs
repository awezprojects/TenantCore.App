using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IAmountHandoverRepository : IClinicRepository<AmountHandover>
{
    Task<IEnumerable<AmountHandover>> GetPendingAsync(Guid applicationId, CancellationToken ct = default);
    Task<IEnumerable<AmountHandover>> GetBySessionIdAsync(Guid sessionId, Guid applicationId, CancellationToken ct = default);
    Task<bool> HasAcceptedHandoverAsync(Guid sessionId, Guid applicationId, CancellationToken ct = default);
    Task<IEnumerable<AmountHandover>> GetByDateRangeAsync(Guid applicationId, DateTime? fromInclusive, DateTime? toExclusive, CancellationToken ct = default);
}
