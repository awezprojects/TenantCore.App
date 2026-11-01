using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Interfaces;

public interface IClinicAccountRepository : IRepository<ClinicAccount>
{
    /// <summary>Tracked — callers mutate the row. Null when the clinic has never needed one (it is then Active with the public catalogue).</summary>
    Task<ClinicAccount?> GetByApplicationIdAsync(Guid applicationId, CancellationToken ct = default);

    /// <summary>No-tracking single-column check used by the request guard on every clinic-scoped call.</summary>
    Task<bool> IsSuspendedAsync(Guid applicationId, CancellationToken ct = default);

    /// <summary>
    /// The <b>effective</b> billing mode, resolved server-side from just three columns. Runs on every
    /// guarded clinic-scoped request, so it is a single no-tracking indexed read. A clinic with no
    /// account row is <see cref="BillingMode.Subscription"/>, which is exactly today's behaviour.
    /// </summary>
    Task<BillingMode> GetBillingModeAsync(Guid applicationId, CancellationToken ct = default);

    /// <summary>
    /// Tracked get-or-create, so a billing action (mode, fee, waive, rates) works on a clinic that has
    /// never needed a row. The caller owns <c>SaveChangesAsync</c>.
    /// </summary>
    Task<ClinicAccount> GetOrCreateAsync(Guid applicationId, CancellationToken ct = default);
}
