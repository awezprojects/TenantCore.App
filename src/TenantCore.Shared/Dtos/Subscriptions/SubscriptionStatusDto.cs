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

    /// <summary>True when an internal admin suspended the clinic. Overrides everything else in the UI.</summary>
    public bool IsSuspended { get; init; }

    /// <summary>The admin's message, shown on the suspended screen. Null unless IsSuspended.</summary>
    public string? SuspensionMessage { get; init; }

    /// <summary>
    /// The last day the clinic is covered, counting terms it has already bought but not started.
    /// Null when nothing covers it. DaysRemaining/IsExpiringSoon are measured against this, not
    /// against the current term, so a clinic that already bought its next term never sees a warning.
    /// </summary>
    public DateTime? CoverageEndDate { get; init; }

    public int CoverageDaysRemaining { get; init; }

    /// <summary>Terms already paid for or granted that start later, earliest first.</summary>
    public IReadOnlyList<UpcomingSubscriptionDto> Upcoming { get; init; } = [];

    /// <summary>True only when the caller is Clinic Admin for the current clinic — drives whether the Subscribe button is shown.</summary>
    public bool CanSubscribe { get; init; }

    public bool HasUsedTrial { get; init; }
}
