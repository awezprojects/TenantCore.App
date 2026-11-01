using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Interfaces;

/// <summary>Per-item rollup for the usage dashboard: what was charged, and what it would have cost.</summary>
public sealed record ClinicUsageTotals(BillingItem BillingItem, int Count, decimal Charged, decimal Notional);

/// <summary>
/// The metered-usage ledger. Append-only, one row per billable operation for every clinic regardless
/// of billing mode.
/// </summary>
public interface IClinicUsageRecordRepository : IRepository<ClinicUsageRecord>
{
    /// <summary>
    /// The idempotency pre-check for a metered operation. The unique index over
    /// (ApplicationId, BillingItem, SourceEntityId) is the backstop for a genuine race.
    /// </summary>
    Task<bool> ExistsAsync(
        Guid applicationId, BillingItem billingItem, Guid sourceEntityId, CancellationToken ct = default);

    /// <summary>
    /// The existing row for a metered operation, so a replay can return the original charge instead of
    /// creating a second one. Null when the operation has never been charged.
    /// </summary>
    Task<ClinicUsageRecord?> GetBySourceAsync(
        Guid applicationId, BillingItem billingItem, Guid sourceEntityId, CancellationToken ct = default);

    /// <summary>Raw rows in a capped range, for the usage detail list.</summary>
    Task<IReadOnlyList<ClinicUsageRecord>> GetInRangeAsync(
        Guid applicationId, DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>Per-item totals in a range — the dashboard's grouped counts, charged and notional.</summary>
    Task<IReadOnlyList<ClinicUsageTotals>> GetTotalsAsync(
        Guid applicationId, DateTime from, DateTime to, CancellationToken ct = default);
}
