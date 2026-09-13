using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;
using TenantCore.Infrastructure.Repositories;

namespace TenantCore.Infrastructure.Caching;

public class CachedMedicineTypeRepository(
    MedicineTypeRepository innerRepository,
    ClinicDbContext dbContext,
    RefreshableCache<MedicineType> cache) : IMedicineTypeRepository
{
    public async Task<(IEnumerable<MedicineType> Items, int Total)> GetPagedAsync(
        int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var all = await GetAllCachedAsync(ct);

        var query = all.Where(t => t.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Name.Contains(search, StringComparison.OrdinalIgnoreCase));

        var ordered = query.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToList();
        var total = ordered.Count;
        var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return (items, total);
    }

    // Not cached — used as the duplicate-name guard on create/update, which must never miss a
    // name added moments earlier by another request.
    public Task<MedicineType?> GetByNameAsync(string name, CancellationToken ct = default)
        => innerRepository.GetByNameAsync(name, ct);

    // Not cached — the entity returned here is mutated in place by Update handlers before being
    // saved, so it must always be a live, unshared instance rather than a cached one.
    public Task<MedicineType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => innerRepository.GetByIdAsync(id, cancellationToken);

    public async Task<IReadOnlyList<MedicineType>> GetAllAsync(CancellationToken cancellationToken = default)
        => await GetAllCachedAsync(cancellationToken);

    public Task AddAsync(MedicineType entity, CancellationToken cancellationToken = default)
        => innerRepository.AddAsync(entity, cancellationToken);

    public void Update(MedicineType entity) => innerRepository.Update(entity);

    public void Delete(MedicineType entity) => innerRepository.Delete(entity);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => innerRepository.ExistsAsync(id, cancellationToken);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await innerRepository.SaveChangesAsync(cancellationToken);

        // Refresh immediately after a successful write — this table is small, so reloading it
        // inline is cheap, and it guarantees the next read never serves stale data. This
        // replaces the old snapshot only once the new one is fully read, so there is never a
        // gap where the cache is empty.
        var refreshed = await MedicineCacheQueries.LoadMedicineTypesAsync(dbContext, cancellationToken);
        cache.Set(refreshed);

        return result;
    }

    private async Task<IReadOnlyList<MedicineType>> GetAllCachedAsync(CancellationToken ct)
        => cache.Current ?? await MedicineCacheQueries.LoadMedicineTypesAsync(dbContext, ct);
}
