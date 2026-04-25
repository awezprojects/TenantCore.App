using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IPatientRepository : IClinicRepository<Patient>
{
    Task<(IEnumerable<Patient> Items, int Total)> GetPagedAsync(
        Guid applicationId, int page, int pageSize, string? search, CancellationToken ct = default);

    Task<Patient?> GetByPhoneAsync(Guid applicationId, string phone, CancellationToken ct = default);
}
