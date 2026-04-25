using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IMedicineTypeRepository : IRepository<MedicineType>
{
    Task<(IEnumerable<MedicineType> Items, int Total)> GetPagedAsync(
        int page, int pageSize, string? search, CancellationToken ct = default);

    Task<MedicineType?> GetByNameAsync(string name, CancellationToken ct = default);
}
