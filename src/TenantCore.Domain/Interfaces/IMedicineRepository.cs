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
        Guid? dosageFormId,
        bool? isGeneric,
        Guid applicationId,
        bool includeInactive = false,
        CancellationToken ct = default);

    Task<Medicine?> GetByIdWithTypeAsync(Guid id, CancellationToken ct = default);

    Task<IEnumerable<Medicine>> FindSimilarAsync(
        string name,
        string? brandName,
        string? dosage,
        Guid applicationId,
        Guid? excludeId = null,
        CancellationToken ct = default);

    Task<IEnumerable<Medicine>> GetUnmappedAsync(int batchSize, CancellationToken ct = default);

    Task<IEnumerable<Medicine>> GetByNamePrefixAsync(string name, Guid applicationId, int limit = 5, CancellationToken ct = default);
}
