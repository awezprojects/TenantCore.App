using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos.Subscriptions;

/// <summary>Full detail of one ClinicSubscription record — returned after subscribing.</summary>
public record ClinicSubscriptionDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public Guid SubscriptionPlanId { get; init; }
    public SubscriptionPlanCode PlanCode { get; init; }
    public string PlanName { get; init; } = string.Empty;
    public decimal PricePaid { get; init; }
    public string Currency { get; init; } = string.Empty;
    public int DurationDays { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public SubscriptionStatus Status { get; init; }
    public string ClinicName { get; init; } = string.Empty;
    public string BillingContactEmail { get; init; } = string.Empty;
    public string BillingContactName { get; init; } = string.Empty;
}
