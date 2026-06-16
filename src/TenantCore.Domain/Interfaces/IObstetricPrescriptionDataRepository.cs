using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IObstetricPrescriptionDataRepository
{
    Task<ObstetricPrescriptionData?> GetByPrescriptionIdAsync(Guid prescriptionId, CancellationToken ct = default);
    Task<ObstetricPrescriptionData?> GetMostRecentWithLmpByPatientIdAsync(Guid patientId, Guid applicationId, CancellationToken ct = default);
    Task<HashSet<Guid>> GetPatientIdsWithLmpAsync(IEnumerable<Guid> patientIds, Guid applicationId, CancellationToken ct = default);
    Task AddAsync(ObstetricPrescriptionData data, CancellationToken ct = default);
    void Update(ObstetricPrescriptionData data);
    Task SaveChangesAsync(CancellationToken ct = default);
}
