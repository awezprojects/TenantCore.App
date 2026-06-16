using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IDoctorSpecialityRepository : IClinicRepository<DoctorSpeciality>
{
    Task<List<DoctorSpeciality>> GetAllActiveAsync(CancellationToken ct = default);
}
