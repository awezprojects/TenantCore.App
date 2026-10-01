namespace TenantCore.Shared.Dtos.Subscriptions;

/// <summary>
/// A term the clinic has already paid for (or been granted) that has not started yet.
/// It begins automatically the moment the current term ends — no gap, no action needed.
/// </summary>
public record UpcomingSubscriptionDto
{
    public Guid Id { get; init; }
    public string PlanName { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public decimal PricePaid { get; init; }
    public string Currency { get; init; } = string.Empty;

    /// <summary>True when an internal admin granted this term for free.</summary>
    public bool IsGrant { get; init; }
}
