using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class MedicineRepository(ClinicDbContext dbContext)
    : ClinicRepository<Medicine>(dbContext), IMedicineRepository
{
    public async Task<(IEnumerable<Medicine> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        string? brandName,
        string? genericName,
        Guid? medicineTypeId,
        bool? isGeneric,
        bool includeInactive = false,
        CancellationToken ct = default)
    {
        var query = DbSet.Include(m => m.MedicineType).AsQueryable();

        if (!includeInactive)
            query = query.Where(m => m.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(m =>
                m.Name.Contains(search) ||
                (m.GenericName != null && m.GenericName.Contains(search)) ||
                (m.BrandName != null && m.BrandName.Contains(search)));

        if (!string.IsNullOrWhiteSpace(brandName))
            query = query.Where(m => m.BrandName != null && m.BrandName.Contains(brandName));

        if (!string.IsNullOrWhiteSpace(genericName))
            query = query.Where(m => m.GenericName != null && m.GenericName.Contains(genericName));

        if (medicineTypeId.HasValue)
            query = query.Where(m => m.MedicineTypeId == medicineTypeId);

        if (isGeneric.HasValue)
            query = query.Where(m => m.IsGeneric == isGeneric.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(m => m.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<Medicine?> GetByIdWithTypeAsync(Guid id, CancellationToken ct = default)
        => await DbSet
            .Include(m => m.MedicineType)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<IEnumerable<Medicine>> FindSimilarAsync(
        string name,
        string? genericName,
        string? brandName,
        Guid? excludeId = null,
        CancellationToken ct = default)
    {
        var nameLower = name.ToLower();

        var query = DbSet.Where(m =>
            m.Name.ToLower().Contains(nameLower) ||
            (genericName != null && m.GenericName != null && m.GenericName.ToLower().Contains(genericName.ToLower())) ||
            (brandName != null && m.BrandName != null && m.BrandName.ToLower().Contains(brandName.ToLower())));

        if (excludeId.HasValue)
            query = query.Where(m => m.Id != excludeId.Value);

        return await query.ToListAsync(ct);
    }
}
