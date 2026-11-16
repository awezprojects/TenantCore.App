using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IIpdParticularRepository : IClinicRepository<IpdParticular>
{
    Task<IEnumerable<IpdParticular>> GetByRegistrationAsync(Guid ipdRegistrationId, Guid applicationId, CancellationToken ct = default);
}
