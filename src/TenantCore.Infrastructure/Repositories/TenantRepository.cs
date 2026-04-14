using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

/// <summary>
/// Repository for Tenant-specific operations.
/// Inherits common CRUD operations from Repository&lt;Tenant&gt;.
/// </summary>
public class TenantRepository(AppDbContext dbContext) : Repository<Tenant>(dbContext), ITenantRepository
{
    public async Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default)
        => await DbSet
            .FirstOrDefaultAsync(t => t.Subdomain == subdomain.ToLowerInvariant(), cancellationToken);

    public async Task<(IReadOnlyList<Tenant> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Name.Contains(search) || t.Subdomain.Contains(search));

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<bool> ExistsBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default)
        => await DbSet.AnyAsync(t => t.Subdomain == subdomain.ToLowerInvariant(), cancellationToken);
}
