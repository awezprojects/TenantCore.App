using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Interfaces;

/// <summary>
/// One admin-set price per billable operation per clinic. Setting a rate is an upsert: the handler
/// deactivates the live row and inserts its replacement in the same SaveChanges.
/// </summary>
public interface IClinicUsageRateRepository : IRepository<ClinicUsageRate>
{
    /// <summary>Every live rate for one clinic, for the rates table on the wallet screen.</summary>
    Task<IReadOnlyList<ClinicUsageRate>> GetActiveForClinicAsync(Guid applicationId, CancellationToken ct = default);

    /// <summary>
    /// The rate in force for one item — <b>tracked</b>, because the upsert deactivates this very row
    /// before inserting its replacement. Null means the item is unpriced, which means free.
    /// </summary>
    Task<ClinicUsageRate?> GetActiveAsync(Guid applicationId, BillingItem billingItem, CancellationToken ct = default);
}
