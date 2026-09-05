using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IPrescriptionRepository : IClinicRepository<Prescription>
{
    Task<(IEnumerable<Prescription> Items, int Total)> GetPagedAsync(
        Guid applicationId, int page, int pageSize, string? search,
        Guid? doctorUserId, Guid? patientId, DateTime? from, DateTime? to,
        CancellationToken ct = default);

    Task<Prescription?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);

    Task<Prescription?> GetByOpdRegistrationIdAsync(Guid opdRegistrationId, CancellationToken ct = default);

    Task<string> GetNextPrescriptionNumberAsync(Guid applicationId, CancellationToken ct = default);
}
