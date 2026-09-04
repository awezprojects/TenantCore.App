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
}
