namespace TenantCore.Shared.Dtos.PlatformAdmin;

/// <summary>Free activation of a plan for an existing clinic. Queued after current coverage.</summary>
public record GrantClinicSubscriptionRequest
{
    public Guid SubscriptionPlanId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public ClinicContactRequest Contact { get; init; } = new();

    public Guid AdminUserId { get; init; }
    public string AdminEmail { get; init; } = string.Empty;
}
