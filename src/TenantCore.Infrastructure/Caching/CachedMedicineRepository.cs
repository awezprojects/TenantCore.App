using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;
using TenantCore.Infrastructure.Repositories;

namespace TenantCore.Infrastructure.Caching;

public class CachedMedicineRepository(
    MedicineRepository innerRepository,
    ClinicDbContext dbContext,
    RefreshableCache<Medicine> systemMedicineCache) : IMedicineRepository
{
    public async Task<(IEnumerable<Medicine> Items, int Total)> GetPagedAsync(
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
        CancellationToken ct = default)
    {
        var systemMedicines = await GetSystemMedicinesAsync(ct);
        var clinicMedicines = await GetClinicMedicinesAsync(applicationId, ct);

        var query = systemMedicines.Concat(clinicMedicines).AsEnumerable();

        if (!includeInactive)
            query = query.Where(m => m.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(m =>
                Contains(m.Name, search) ||
                Contains(m.GenericName, search) ||
                Contains(m.BrandName, search) ||
                Contains(m.Manufacturer, search));

        if (!string.IsNullOrWhiteSpace(brandName))
            query = query.Where(m => Contains(m.BrandName, brandName));

        if (!string.IsNullOrWhiteSpace(genericName))
            query = query.Where(m => Contains(m.GenericName, genericName));

        if (medicineTypeId.HasValue)
            query = query.Where(m => m.MedicineTypeId == medicineTypeId);

        if (isGeneric.HasValue)
            query = query.Where(m => m.IsGeneric == isGeneric.Value);

        if (dosageFormId.HasValue)
            query = query.Where(m => m.DosageFormId == dosageFormId.Value);

        var ordered = query.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase).ToList();
        var total = ordered.Count;
        var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return (items, total);
    }

    public async Task<Medicine?> GetByIdWithTypeAsync(Guid id, CancellationToken ct = default)
    {
        var systemMedicines = await GetSystemMedicinesAsync(ct);
        var cached = systemMedicines.FirstOrDefault(m => m.Id == id);
        if (cached is not null)
            return cached;

        return await innerRepository.GetByIdWithTypeAsync(id, ct);
    }

    public async Task<IEnumerable<Medicine>> GetByNamePrefixAsync(
        string name, Guid applicationId, int limit = 5, CancellationToken ct = default)
    {
        var systemMedicines = await GetSystemMedicinesAsync(ct);
        var clinicMedicines = await GetClinicMedicinesAsync(applicationId, ct);

        return systemMedicines.Concat(clinicMedicines)
            .Where(m => m.IsActive && m.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
            .OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList();
    }

    // Not cached — an exact-duplicate check called only on save, and it must never miss a
    // duplicate created moments earlier by another clinic.
    public Task<IEnumerable<Medicine>> FindSimilarAsync(
        string name, string? brandName, string? dosage, Guid applicationId, Guid? excludeId = null, CancellationToken ct = default)
        => innerRepository.FindSimilarAsync(name, brandName, dosage, applicationId, excludeId, ct);

    public Task<IEnumerable<Medicine>> GetUnmappedAsync(int batchSize, CancellationToken ct = default)
        => innerRepository.GetUnmappedAsync(batchSize, ct);

    public Task<Medicine?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => innerRepository.GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<Medicine>> GetAllAsync(CancellationToken cancellationToken = default)
        => innerRepository.GetAllAsync(cancellationToken);

    public Task AddAsync(Medicine entity, CancellationToken cancellationToken = default)
        => innerRepository.AddAsync(entity, cancellationToken);

    public void Update(Medicine entity) => innerRepository.Update(entity);

    public void Delete(Medicine entity) => innerRepository.Delete(entity);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => innerRepository.ExistsAsync(id, cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => innerRepository.SaveChangesAsync(cancellationToken);

    // Served from the background-refreshed snapshot (see MedicineCacheWarmupService). If it
    // hasn't warmed up yet — only possible in the brief window right after a cold start — this
    // query runs live for just this one call instead of blocking the request on a full rebuild.
    private async Task<IReadOnlyList<Medicine>> GetSystemMedicinesAsync(CancellationToken ct)
        => systemMedicineCache.Current ?? await MedicineCacheQueries.LoadSystemMedicinesAsync(dbContext, ct);

    private async Task<List<Medicine>> GetClinicMedicinesAsync(Guid applicationId, CancellationToken ct)
        => await dbContext.Set<Medicine>()
            .AsNoTracking()
            .Include(m => m.MedicineType)
            .Include(m => m.DosageForm)
            .Where(m => m.ApplicationId == applicationId)
            .ToListAsync(ct);

    private static bool Contains(string? value, string search)
        => value is not null && value.Contains(search, StringComparison.OrdinalIgnoreCase);
}
