using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IClinicFeatureFlagsRepository : IClinicRepository<ClinicFeatureFlags>
{
    Task<ClinicFeatureFlags?> GetByApplicationAsync(Guid applicationId, CancellationToken ct = default);
}
