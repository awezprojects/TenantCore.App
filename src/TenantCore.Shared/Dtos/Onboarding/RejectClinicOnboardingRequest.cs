namespace TenantCore.Shared.Dtos.Onboarding;

/// <summary>Internal — sent by the Admin portal via the InternalService scheme.</summary>
public record RejectClinicOnboardingRequest
{
    public string Reason { get; init; } = string.Empty;
    public Guid AdminUserId { get; init; }
    public string AdminEmail { get; init; } = string.Empty;
}
