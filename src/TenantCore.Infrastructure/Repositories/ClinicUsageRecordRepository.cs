using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Repositories;

public class ClinicUsageRecordRepository(ClinicDbContext dbContext)
    : ClinicRepository<ClinicUsageRecord>(dbContext), IClinicUsageRecordRepository
{
    public async Task<bool> ExistsAsync(
        Guid applicationId, BillingItem billingItem, Guid sourceEntityId, CancellationToken ct = default)
        => await DbSet.AsNoTracking()
            .AnyAsync(u => u.ApplicationId == applicationId
                        && u.BillingItem == billingItem
                        && u.SourceEntityId == sourceEntityId, ct);

    public async Task<ClinicUsageRecord?> GetBySourceAsync(
        Guid applicationId, BillingItem billingItem, Guid sourceEntityId, CancellationToken ct = default)
        => await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(u => u.ApplicationId == applicationId
                                   && u.BillingItem == billingItem
                                   && u.SourceEntityId == sourceEntityId, ct);

    public async Task<IReadOnlyList<ClinicUsageRecord>> GetInRangeAsync(
        Guid applicationId, DateTime from, DateTime to, CancellationToken ct = default)
        => await DbSet.AsNoTracking()
            .Where(u => u.ApplicationId == applicationId && u.ChargedAt >= from && u.ChargedAt <= to)
            .OrderByDescending(u => u.ChargedAt)
            .ThenByDescending(u => u.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ClinicUsageTotals>> GetTotalsAsync(
        Guid applicationId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        // Grouped and summed in SQL, then shaped in memory — the record projection is kept out of the
        // query so nothing depends on how the provider translates a positional constructor.
        var rows = await DbSet.AsNoTracking()
            .Where(u => u.ApplicationId == applicationId && u.ChargedAt >= from && u.ChargedAt <= to)
            .GroupBy(u => u.BillingItem)
            .Select(g => new
            {
                BillingItem = g.Key,
                Count = g.Count(),
                Charged = g.Sum(x => x.Amount),
                Notional = g.Sum(x => x.UnitRate * x.Quantity)
            })
            .ToListAsync(ct);

        return rows
            .Select(r => new ClinicUsageTotals(r.BillingItem, r.Count, r.Charged, r.Notional))
            .ToList();
    }
}
