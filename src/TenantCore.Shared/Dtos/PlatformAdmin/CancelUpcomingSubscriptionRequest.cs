namespace TenantCore.Shared.Dtos.PlatformAdmin;

public record CancelUpcomingSubscriptionRequest
{
    public string Reason { get; init; } = string.Empty;

    public Guid AdminUserId { get; init; }
    public string AdminEmail { get; init; } = string.Empty;
}
