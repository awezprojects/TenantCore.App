using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class MedicineDosageFormRepository(ClinicDbContext dbContext)
    : ClinicRepository<MedicineDosageForm>(dbContext), IMedicineDosageFormRepository
{
    public async Task<(IEnumerable<MedicineDosageForm> Items, int Total)> GetPagedAsync(
        int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = DbSet.Where(f => f.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(f => f.Name.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(f => f.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<MedicineDosageForm?> GetByNameAsync(string name, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(
            f => f.Name.ToLower() == name.ToLower(), ct);
}
