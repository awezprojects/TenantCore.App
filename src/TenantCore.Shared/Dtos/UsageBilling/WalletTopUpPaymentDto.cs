using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos.UsageBilling;

/// <summary>
/// The result of asking for a wallet top-up link. <see cref="PaymentLinkUrl"/> is only populated once
/// the background task has created the link at the gateway — the client polls by asking for a check.
/// </summary>
public record WalletTopUpPaymentDto
{
    public Guid PaymentId { get; init; }
    public decimal Amount { get; init; }
    public string? PaymentLinkUrl { get; init; }
    public SubscriptionPaymentStatus Status { get; init; }
}
