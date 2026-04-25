using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IClinicFeeConfigRepository : IClinicRepository<ClinicFeeConfig>
{
    Task<ClinicFeeConfig?> GetByApplicationAsync(Guid applicationId, CancellationToken ct = default);
}
