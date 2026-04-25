using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class MedicineTypeRepository(ClinicDbContext dbContext)
    : ClinicRepository<MedicineType>(dbContext), IMedicineTypeRepository
{
    public async Task<(IEnumerable<MedicineType> Items, int Total)> GetPagedAsync(
        int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = DbSet.Where(t => t.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Name.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<MedicineType?> GetByNameAsync(string name, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(
            t => t.Name.ToLower() == name.ToLower(), ct);
}
