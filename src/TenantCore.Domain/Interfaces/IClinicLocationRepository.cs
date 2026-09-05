using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IClinicLocationRepository : IClinicRepository<ClinicLocation>
{
    Task<ClinicLocation?> GetByApplicationAsync(Guid applicationId, CancellationToken ct = default);
}
