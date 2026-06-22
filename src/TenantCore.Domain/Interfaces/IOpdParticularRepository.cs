using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IOpdParticularRepository : IClinicRepository<OpdParticular>
{
    Task<IEnumerable<OpdParticular>> GetByOpdRegistrationIdAsync(Guid opdRegistrationId, Guid applicationId, CancellationToken ct = default);
    Task<decimal> GetTotalByOpdRegistrationIdAsync(Guid opdRegistrationId, Guid applicationId, CancellationToken ct = default);

    // Returns all service items collected in a given counter session (CounterSessionId set, Received).
    // Used by counter handlers to compute the per-item portion of the session total.
    Task<IEnumerable<OpdParticular>> GetCollectedBySessionIdAsync(Guid sessionId, Guid applicationId, CancellationToken ct = default);
}
