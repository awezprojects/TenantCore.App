namespace TenantCore.Shared.Dtos.PlatformAdmin;

/// <summary>
/// Internal — admin sending (or re-sending) the clinic's one-time registration fee payment link.
/// The fee amount is resolved server-side; admin cannot override it here.
/// The Admin resolves the clinic contact from Auth before calling this endpoint so the handler
/// does not need to make its own Auth API call (which would fail — no user JWT on service-to-service).
/// </summary>
public record SendRegistrationFeeLinkRequest
{
    public Guid AdminUserId { get; init; }
    public string AdminEmail { get; init; } = string.Empty;

    // Contact resolved by the Admin portal from Auth DB (read-only) before the call.
    public string ContactClinicName { get; init; } = string.Empty;
    public string ContactName { get; init; } = string.Empty;
    public string ContactEmail { get; init; } = string.Empty;
    public string? ContactPhone { get; init; }
}
