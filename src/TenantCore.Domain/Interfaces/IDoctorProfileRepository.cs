using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IDoctorProfileRepository : IClinicRepository<DoctorProfile>
{
    Task<DoctorProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
}
