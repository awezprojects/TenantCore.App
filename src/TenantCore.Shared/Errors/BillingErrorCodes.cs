namespace TenantCore.Shared.Errors;

/// <summary>
/// Stable string code surfaced to the Blazor client via ProblemDetails when a
/// clinic has disabled billing, so the UI can key behaviour off a code rather
/// than parsing a human-readable message.
/// </summary>
public static class BillingErrorCodes
{
    public const string BillingDisabled = "billing_disabled";
}
