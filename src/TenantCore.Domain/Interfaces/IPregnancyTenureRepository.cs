using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IPregnancyTenureRepository : IClinicRepository<PregnancyTenure>
{
    Task<PregnancyTenure?> GetActiveForPatientAsync(Guid patientId, Guid applicationId, CancellationToken ct = default);
    Task<IEnumerable<PregnancyTenure>> GetAllOverdueAsync(Guid applicationId, DateOnly today, CancellationToken ct = default);
    Task<IEnumerable<PregnancyTenure>> GetAllForPatientAsync(Guid patientId, Guid applicationId, CancellationToken ct = default);
    Task<Dictionary<Guid, bool>> GetTenureInfoForPatientsAsync(IEnumerable<Guid> patientIds, Guid applicationId, CancellationToken ct = default);
}
