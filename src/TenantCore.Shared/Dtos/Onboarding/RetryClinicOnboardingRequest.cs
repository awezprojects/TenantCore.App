namespace TenantCore.Shared.Dtos.Onboarding;

/// <summary>Internal — retries a NeedsAttention request, optionally with a corrected clinic code.</summary>
public record RetryClinicOnboardingRequest
{
    public string? ClinicCode { get; init; }
    public Guid AdminUserId { get; init; }
    public string AdminEmail { get; init; } = string.Empty;
}
