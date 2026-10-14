using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos.UsageBilling;

/// <summary>
/// Everything the billing tab needs in one read: which mode applies, what the wallet holds, what the
/// registration fee is, and the live metered rates. <see cref="EffectiveBillingMode"/> is the single
/// server-side answer to "which mode is live right now" — the client never recomputes it.
/// </summary>
public record ClinicBillingSummaryDto
{
    /// <summary>The stored mode — what the clinic is set to, before any queued change applies.</summary>
    public BillingMode BillingMode { get; init; }

    /// <summary>The mode actually in force today, after the queued-change rule is applied.</summary>
    public BillingMode EffectiveBillingMode { get; init; }

    /// <summary>A change queued to take effect at the end of current coverage, if any.</summary>
    public BillingMode? PendingBillingMode { get; init; }

    public DateTime? PendingBillingModeEffectiveFrom { get; init; }

    /// <summary>The last moment the clinic is covered by a subscription, counting terms not yet started.</summary>
    public DateTime? CoverageEnd { get; init; }

    public string Currency { get; init; } = "INR";

    /// <summary>Null when no wallet row exists yet — i.e. no credit has ever been granted.</summary>
    public decimal? WalletBalance { get; init; }

    /// <summary>Balance at or below the threshold. A warning only; it never blocks anything.</summary>
    public bool WalletIsLow { get; init; }

    public decimal? LowBalanceThreshold { get; init; }

    public IReadOnlyList<UsageRateDto> Rates { get; init; } = [];

    /// <summary>True only when the fee is unresolved: not waived, never charged, and non-zero.</summary>
    public bool RegistrationFeeDue { get; init; }

    /// <summary>The fee that applies to this clinic — its own override, else the plan default. Zero means none.</summary>
    public decimal RegistrationFee { get; init; }

    public bool RegistrationFeeWaived { get; init; }

    public DateTime? RegistrationFeeChargedAt { get; init; }

    /// <summary>The clinic's granted free-test-days override, if an admin set one.</summary>
    public int? FreeTestDays { get; init; }

    /// <summary>Set the first time a trial term is created. A clinic with this set can never get a second trial.</summary>
    public DateTime? TrialStartedAt { get; init; }
}
