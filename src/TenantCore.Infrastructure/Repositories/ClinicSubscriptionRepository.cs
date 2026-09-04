using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Repositories;

public class ClinicSubscriptionRepository(ClinicDbContext dbContext)
    : ClinicRepository<ClinicSubscription>(dbContext), IClinicSubscriptionRepository
{
    public async Task<ClinicSubscription?> GetActiveForClinicAsync(Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .Where(s => s.ApplicationId == applicationId
                     && s.Status == SubscriptionStatus.Active
                     && s.EndDate >= DateTime.UtcNow)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync(ct);

    public async Task<ClinicSubscription?> GetLatestForClinicAsync(Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .Where(s => s.ApplicationId == applicationId)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<ClinicSubscription>> GetHistoryForClinicAsync(Guid applicationId, CancellationToken ct = default)
        => await DbSet.AsNoTracking()
            .Where(s => s.ApplicationId == applicationId)
            .OrderByDescending(s => s.StartDate)
            .ToListAsync(ct);

    public async Task<bool> HasUsedTrialAsync(Guid applicationId, CancellationToken ct = default)
        => await DbSet.AsNoTracking()
            .AnyAsync(s => s.ApplicationId == applicationId
                        && s.PlanCode == SubscriptionPlanCode.Trial, ct);
}
