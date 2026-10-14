namespace TenantCore.Shared.Errors;

/// <summary>
/// Stable string codes for usage-billing failures, surfaced to the Blazor client via
/// ProblemDetails so it can key UI behaviour off a code rather than parsing a message.
///
/// Deliberately separate from <see cref="SubscriptionErrorCodes"/>: the word "Billing" is
/// already taken in this repo by the clinic's own patient-facing billing module
/// (<c>BillingErrorCodes</c> / <c>BillingFeatureGuardMiddleware</c>), and platform billing
/// must never be confused with it. One code lives in exactly one class — there is no
/// pass-through re-export of these constants anywhere.
/// </summary>
public static class UsageBillingErrorCodes
{
    /// <summary>403 from the metered creates — the wallet cannot cover the operation. The client opens the top-up panel.</summary>
    public const string InsufficientWalletBalance = "insufficient_wallet_balance";

    /// <summary>Carried in the summary DTO as a warning flag, never as an error.</summary>
    public const string WalletBalanceLow = "wallet_balance_low";

    /// <summary>409 — a different billing-mode change is already queued and unresolved.</summary>
    public const string BillingModeLocked = "billing_mode_locked";

    /// <summary>409 — a top-up link is already open; the existing link is returned instead.</summary>
    public const string TopUpLinkOpen = "topup_link_open";
}
