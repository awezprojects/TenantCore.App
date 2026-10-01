using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Repositories;

public class SubscriptionPlanRepository(ClinicDbContext dbContext)
    : ClinicRepository<SubscriptionPlan>(dbContext), ISubscriptionPlanRepository
{
    public async Task<IReadOnlyList<SubscriptionPlan>> GetActivePlansAsync(CancellationToken ct = default)
        => await DbSet.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync(ct);

    public async Task<SubscriptionPlan?> GetByCodeAsync(SubscriptionPlanCode code, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(p => p.Code == code, ct);

    public async Task<IReadOnlyList<SubscriptionPlan>> GetAllPlansAsync(CancellationToken ct = default)
        => await DbSet.AsNoTracking()
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name)
            .ToListAsync(ct);

    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken ct = default)
    {
        // The database collation is case-insensitive, so plain equality is the case-insensitive
        // comparison here — EF.Functions.Like would add wildcard-escaping concerns for no gain.
        var query = DbSet.AsNoTracking().Where(p => p.Name == name);

        if (excludeId is { } id)
            query = query.Where(p => p.Id != id);

        return await query.AnyAsync(ct);
    }
}
