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
    {
        var utcNow = DateTime.UtcNow;

        // StartDate <= now is essential: a term bought mid-term is queued for later, and must not
        // be reported as the current plan (nor unlock the clinic) before it actually begins.
        // Ordered by StartDate descending so that at an exact boundary the newer term wins.
        return await DbSet
            .Where(s => s.ApplicationId == applicationId
                     && s.Status == SubscriptionStatus.Active
                     && s.StartDate <= utcNow
                     && s.EndDate >= utcNow)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<ClinicSubscription>> GetUpcomingForClinicAsync(
        Guid applicationId, DateTime utcNow, CancellationToken ct = default)
        => await DbSet
            .Where(s => s.ApplicationId == applicationId
                     && s.Status == SubscriptionStatus.Active
                     && s.StartDate > utcNow)
            .OrderBy(s => s.StartDate)
            .ToListAsync(ct);

    public async Task<DateTime?> GetCoverageEndAsync(Guid applicationId, DateTime utcNow, CancellationToken ct = default)
    {
        // Max EndDate over every Active term that has not already finished — current AND upcoming.
        // Nullable projection so an empty set comes back as null rather than default(DateTime).
        var coverageEnd = await DbSet.AsNoTracking()
            .Where(s => s.ApplicationId == applicationId
                     && s.Status == SubscriptionStatus.Active
                     && s.EndDate >= utcNow)
            .Select(s => (DateTime?)s.EndDate)
            .MaxAsync(ct);

        return coverageEnd;
    }

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

    public async Task<bool> HasAnySubscriptionHistoryAsync(Guid applicationId, CancellationToken ct = default)
        => await DbSet.AsNoTracking()
            .AnyAsync(s => s.ApplicationId == applicationId, ct);

    public async Task<ClinicSubscription?> GetByPaymentIdAsync(Guid subscriptionPaymentId, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(s => s.SubscriptionPaymentId == subscriptionPaymentId, ct);

    public async Task<ClinicSubscription?> GetByOnboardingRequestIdAsync(Guid onboardingRequestId, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(s => s.OnboardingRequestId == onboardingRequestId, ct);
}
