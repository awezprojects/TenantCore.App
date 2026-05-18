using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IPrescriptionConfigRepository : IClinicRepository<PrescriptionConfig>
{
    Task<PrescriptionConfig?> GetByApplicationIdAsync(Guid applicationId, CancellationToken ct = default);
}
