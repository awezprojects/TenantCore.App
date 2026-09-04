namespace TenantCore.Shared.Dtos.Subscriptions;

/// <summary>
/// POST body for subscribing a clinic to a plan. Dates, price and duration are
/// always derived server-side from the plan row — never trusted from the client.
/// </summary>
public record SubscribeRequest
{
    public Guid SubscriptionPlanId { get; init; }
}
