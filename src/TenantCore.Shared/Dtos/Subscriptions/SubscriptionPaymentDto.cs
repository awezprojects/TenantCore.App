using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos.Subscriptions;

public record SubscriptionPaymentDto
{
    public Guid Id { get; init; }
    public PaymentPurpose Purpose { get; init; }
    public string PlanName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal PlanListPrice { get; init; }
    public string Currency { get; init; } = string.Empty;
    public SubscriptionPaymentStatus Status { get; init; }
    public string? Method { get; init; }
    public string? PaymentLinkUrl { get; init; }
    public DateTime? LinkExpiresAt { get; init; }
    public DateTime? PaidAt { get; init; }
    public DateTime CreatedAt { get; init; }

    /// <summary>True for a link an internal admin sent (Purpose == AdminAssigned) — the clinic cannot swap its plan.</summary>
    public bool IsAssignedByPlatform { get; init; }
}
