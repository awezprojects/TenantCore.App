namespace TenantCore.Application.Features.UsageBilling.Services;

/// <summary>
/// Tunables for pay-as-you-go metering, bound to the <c>UsageBilling</c> configuration section.
/// Every value has a safe default, so the feature runs with no configuration present at all.
///
/// This lives in the Application layer on purpose: the metering handler reads <see cref="Enabled"/>
/// and Infrastructure references Application, never the reverse — the same reason
/// <c>WorkflowOptions</c> and <c>OnboardingOptions</c> live here.
/// </summary>
public sealed class UsageBillingOptions
{
    public const string SectionName = "UsageBilling";

    /// <summary>
    /// Master switch for metering. Off means every usage row is still written, but free
    /// (<c>IsFree = true</c>, <c>Amount = 0</c>) — never "no row" — so analytics keep working while a
    /// rollout is paused and no clinic is ever charged mid-rollback.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Currency recorded on a wallet when it is first created.</summary>
    public string Currency { get; set; } = "INR";

    /// <summary>Low-balance warning threshold for a newly created wallet. Informational only — it never blocks.</summary>
    public decimal DefaultLowBalanceThreshold { get; set; } = 100.00m;

    /// <summary>Smallest accepted top-up. Enforced on both clinic- and admin-initiated top-ups.</summary>
    public decimal TopUpMinAmount { get; set; } = 100m;

    /// <summary>Largest accepted top-up, so a typo cannot create an absurd payment link.</summary>
    public decimal TopUpMaxAmount { get; set; } = 100000m;

    /// <summary>Throttle for the "I've paid — check now" action, matching Phase 1's subscription check.</summary>
    public int PaymentCheckThrottleSeconds { get; set; } = 30;

    /// <summary>
    /// Most usage rows one query returns. The totals and the series are computed from the same rows,
    /// so this caps what a very busy clinic loads into memory without ever hiding the fact that it
    /// capped — the client shows "showing the latest N" instead.
    /// </summary>
    public int MaxUsageRows { get; set; } = 500;

    /// <summary>How far back the usage screen looks when the caller supplies no range.</summary>
    public int DefaultUsageRangeDays { get; set; } = 30;
}
