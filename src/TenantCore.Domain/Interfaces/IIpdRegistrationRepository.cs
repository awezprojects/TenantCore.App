using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IIpdRegistrationRepository : IClinicRepository<IpdRegistration>
{
    Task<(IEnumerable<IpdRegistration> Items, int Total)> GetPagedAsync(
        Guid applicationId, int page, int pageSize, string? search, CancellationToken ct = default);

    Task<string> GetNextAdmissionNumberAsync(Guid applicationId, CancellationToken ct = default);
    Task<bool> HasActiveAdmissionAsync(Guid patientId, Guid applicationId, CancellationToken ct = default);
}
