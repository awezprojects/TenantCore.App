using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface ICounterSessionRepository : IClinicRepository<CounterSession>
{
    Task<CounterSession?> GetActiveSessionAsync(Guid applicationId, CancellationToken ct = default);
    Task<IReadOnlyList<CounterSession>> GetAllActiveAsync(Guid applicationId, CancellationToken ct = default);
    Task<CounterSession?> GetByDateAsync(DateTime date, Guid applicationId, CancellationToken ct = default);
}
