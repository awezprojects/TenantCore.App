using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class TenantRepository(AppDbContext dbContext) : ITenantRepository
{
    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Tenants.FindAsync([id], cancellationToken);

    public async Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default)
        => await dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Subdomain == subdomain.ToLowerInvariant(), cancellationToken);

    public async Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken cancellationToken = default)
        => await dbContext.Tenants.ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Tenant> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Tenants.AsQueryable();

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

    public async Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
        => await dbContext.Tenants.AddAsync(tenant, cancellationToken);

    public void Update(Tenant tenant)
        => dbContext.Tenants.Update(tenant);

    public void Delete(Tenant tenant)
        => dbContext.Tenants.Remove(tenant);

    public async Task<bool> ExistsBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default)
        => await dbContext.Tenants.AnyAsync(t => t.Subdomain == subdomain.ToLowerInvariant(), cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
