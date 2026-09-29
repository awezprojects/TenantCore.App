namespace TenantCore.Shared.Dtos.Subscriptions;

public record CreateRenewalPaymentLinkRequest
{
    public Guid SubscriptionPlanId { get; init; }
}
