using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IMedicineRepository : IRepository<Medicine>
{
    Task<(IEnumerable<Medicine> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        string? brandName,
        string? genericName,
        Guid? medicineTypeId,
        bool? isGeneric,
        bool includeInactive = false,
        CancellationToken ct = default);

    Task<Medicine?> GetByIdWithTypeAsync(Guid id, CancellationToken ct = default);

    Task<IEnumerable<Medicine>> FindSimilarAsync(
        string name,
        string? genericName,
        string? brandName,
        Guid? excludeId = null,
        CancellationToken ct = default);
}
