using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IDoctorFeeConfigRepository : IClinicRepository<DoctorFeeConfig>
{
    Task<DoctorFeeConfig?> GetByDoctorProfileIdAsync(Guid doctorProfileId, Guid applicationId, CancellationToken ct = default);
}
