using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.UsageBilling.Services;

/// <summary>
/// The billing rules the Wallet and Billing screens and the pay-as-you-go handlers all share, so
/// "which mode is live", "what does this operation cost" and "what fee does this clinic owe" have
/// exactly one answer each.
///
/// Nothing here writes. The one thing that decides a real charge — <c>ChargeUsageCommandHandler</c> —
/// keeps its own read-then-debit sequence, because that has to stay inside the parent's transaction.
/// </summary>
public interface IUsageBillingService
{
    /// <summary>
    /// The mode in force at this instant, applying any queued change whose effective time has passed.
    /// A clinic with no account row is <see cref="BillingMode.Subscription"/>.
    /// </summary>
    Task<BillingMode> ResolveEffectiveModeAsync(Guid applicationId, CancellationToken ct = default);

    /// <summary>The live rate for one operation, or 0 when it is unpriced.</summary>
    Task<decimal> ResolveRateAsync(Guid applicationId, BillingItem billingItem, CancellationToken ct = default);

    /// <summary>Every live rate for the clinic, keyed by operation — the rates table's single read.</summary>
    Task<IReadOnlyDictionary<BillingItem, decimal>> ResolveRatesAsync(Guid applicationId, CancellationToken ct = default);

    /// <summary>The clinic's overridden fee if it has one, otherwise the catalogue default.</summary>
    decimal ResolveRegistrationFee(decimal? clinicOverride, decimal planDefaultFee);

    /// <summary>At or below the threshold. A warning only; never a block.</summary>
    bool IsLowBalance(decimal? balance, decimal? threshold);

    /// <summary>Rate × quantity, rounded to currency precision, or 0 for an unpriced item.</summary>
    decimal ResolveChargeableAmount(decimal resolvedRate, int quantity);
}
