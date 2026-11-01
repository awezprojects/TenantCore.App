namespace TenantCore.Shared.Errors;

/// <summary>
/// Stable string codes for subscription-related failures, surfaced to the
/// Blazor client via ProblemDetails so it can key UI behaviour off a code
/// rather than parsing a human-readable message.
/// </summary>
public static class SubscriptionErrorCodes
{
    public const string SubscriptionRequired = "subscription_required";

    /// <summary>403 from SubscriptionGuardMiddleware — the clinic was suspended by an internal admin.</summary>
    public const string ClinicSuspended = "clinic_suspended";

    public const string TrialAlreadyUsed = "trial_already_used";
    public const string SubscriptionAlreadyActive = "subscription_already_active";
}
