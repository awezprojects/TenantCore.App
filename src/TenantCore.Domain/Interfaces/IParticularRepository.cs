using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IParticularRepository : IClinicRepository<Particular>
{
    Task<IEnumerable<Particular>> GetActiveAsync(Guid applicationId, CancellationToken ct = default);
}
