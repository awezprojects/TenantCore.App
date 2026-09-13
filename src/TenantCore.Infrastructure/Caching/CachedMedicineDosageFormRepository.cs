using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;
using TenantCore.Infrastructure.Repositories;

namespace TenantCore.Infrastructure.Caching;

public class CachedMedicineDosageFormRepository(
    MedicineDosageFormRepository innerRepository,
    ClinicDbContext dbContext,
    RefreshableCache<MedicineDosageForm> cache) : IMedicineDosageFormRepository
{
    public async Task<(IEnumerable<MedicineDosageForm> Items, int Total)> GetPagedAsync(
        int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var all = await GetAllCachedAsync(ct);

        var query = all.Where(f => f.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(f => f.Name.Contains(search, StringComparison.OrdinalIgnoreCase));

        var ordered = query.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList();
        var total = ordered.Count;
        var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return (items, total);
    }

    // Not cached — used as the duplicate-name guard on create, which must never miss a name
    // added moments earlier by another request.
    public Task<MedicineDosageForm?> GetByNameAsync(string name, CancellationToken ct = default)
        => innerRepository.GetByNameAsync(name, ct);

    // Not cached — the entity returned here is mutated in place by Update/Delete handlers before
    // being saved, so it must always be a live, unshared instance rather than a cached one.
    public Task<MedicineDosageForm?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => innerRepository.GetByIdAsync(id, cancellationToken);

    public async Task<IReadOnlyList<MedicineDosageForm>> GetAllAsync(CancellationToken cancellationToken = default)
        => await GetAllCachedAsync(cancellationToken);

    public Task AddAsync(MedicineDosageForm entity, CancellationToken cancellationToken = default)
        => innerRepository.AddAsync(entity, cancellationToken);

    public void Update(MedicineDosageForm entity) => innerRepository.Update(entity);

    public void Delete(MedicineDosageForm entity) => innerRepository.Delete(entity);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => innerRepository.ExistsAsync(id, cancellationToken);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await innerRepository.SaveChangesAsync(cancellationToken);

        // Refresh immediately after a successful write — small table, cheap to reload inline —
        // swapping in the new snapshot only once it's fully read, so there's never a gap.
        var refreshed = await MedicineCacheQueries.LoadMedicineDosageFormsAsync(dbContext, cancellationToken);
        cache.Set(refreshed);

        return result;
    }

    private async Task<IReadOnlyList<MedicineDosageForm>> GetAllCachedAsync(CancellationToken ct)
        => cache.Current ?? await MedicineCacheQueries.LoadMedicineDosageFormsAsync(dbContext, ct);
}
