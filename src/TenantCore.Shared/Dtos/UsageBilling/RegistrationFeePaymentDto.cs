using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos.UsageBilling;

/// <summary>
/// The result of asking for the one-time registration fee's payment link. Same shape as a top-up:
/// the fee has no plan behind it, so there is nothing else to report.
/// </summary>
public record RegistrationFeePaymentDto
{
    public Guid PaymentId { get; init; }
    public decimal Amount { get; init; }
    public string? PaymentLinkUrl { get; init; }
    public SubscriptionPaymentStatus Status { get; init; }
    /// <summary>UTC expiry of the Razorpay link; null when the link has not been created yet.</summary>
    public DateTime? LinkExpiresAt { get; init; }
    public bool IsExpired => Status == SubscriptionPaymentStatus.Expired;
    public bool IsOpen => Status is SubscriptionPaymentStatus.Pending or SubscriptionPaymentStatus.LinkCreated;
}
