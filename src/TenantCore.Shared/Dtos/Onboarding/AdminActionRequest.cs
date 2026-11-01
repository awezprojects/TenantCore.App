namespace TenantCore.Shared.Dtos.Onboarding;

/// <summary>Internal — the minimal admin-identity body for actions with no other input (resend link, retry a task).</summary>
public record AdminActionRequest
{
    public Guid AdminUserId { get; init; }
    public string AdminEmail { get; init; } = string.Empty;
}
