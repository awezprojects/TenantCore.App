using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos.Subscriptions;

/// <summary>Lean row for the subscription history list.</summary>
public record SubscriptionHistoryItemDto
{
    public Guid Id { get; init; }
    public string PlanName { get; init; } = string.Empty;
    public decimal PricePaid { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public SubscriptionStatus Status { get; init; }
}
