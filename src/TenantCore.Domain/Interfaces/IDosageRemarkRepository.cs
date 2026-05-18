using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Interfaces;

public interface IDosageRemarkRepository : IClinicRepository<DosageRemark>
{
    Task<(IEnumerable<DosageRemark> Items, int Total)> GetPagedAsync(
        Guid applicationId, int page, int pageSize, MedicineFormType? form,
        CancellationToken ct = default);
}
