using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IDoctorProfileRepository : IClinicRepository<DoctorProfile>
{
    Task<DoctorProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IEnumerable<DoctorProfile>> GetByUserIdsAsync(IEnumerable<Guid> userIds, CancellationToken ct = default);
    Task<IEnumerable<DoctorProfile>> GetByIdsAsync(IEnumerable<Guid> profileIds, CancellationToken ct = default);
}
