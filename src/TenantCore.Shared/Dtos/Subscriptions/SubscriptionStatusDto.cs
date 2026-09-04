using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos.Subscriptions;

/// <summary>
/// The subscription guard's answer for one clinic — drives both the API guard
/// and the client-side gate rendered by AuthorizedLayout.
/// </summary>
public record SubscriptionStatusDto
{
    public bool HasActiveSubscription { get; init; }
    public Guid? SubscriptionId { get; init; }
    public SubscriptionPlanCode? PlanCode { get; init; }
    public string? PlanName { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public int DaysRemaining { get; init; }
    public bool IsExpiringSoon { get; init; }

    /// <summary>True only when the caller is Clinic Admin for the current clinic — drives whether the Subscribe button is shown.</summary>
    public bool CanSubscribe { get; init; }

    public bool HasUsedTrial { get; init; }
}
