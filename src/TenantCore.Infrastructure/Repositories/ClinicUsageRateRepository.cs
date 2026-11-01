using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Repositories;

public class ClinicUsageRateRepository(ClinicDbContext dbContext)
    : ClinicRepository<ClinicUsageRate>(dbContext), IClinicUsageRateRepository
{
    public async Task<IReadOnlyList<ClinicUsageRate>> GetActiveForClinicAsync(
        Guid applicationId, CancellationToken ct = default)
        => await DbSet.AsNoTracking()
            .Where(r => r.ApplicationId == applicationId && r.IsActive)
            .OrderBy(r => r.BillingItem)
            .ToListAsync(ct);

    /// <summary>Tracked — the upsert deactivates this exact row before inserting its replacement.</summary>
    public async Task<ClinicUsageRate?> GetActiveAsync(
        Guid applicationId, BillingItem billingItem, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(r => r.ApplicationId == applicationId
                                             && r.BillingItem == billingItem
                                             && r.IsActive, ct);
}
