namespace TenantCore.Shared.Dtos.PlatformAdmin;

/// <summary>
/// An admin-sent Razorpay payment link for an existing clinic. AmountReason is required when
/// Amount differs from the clinic's effective price (its live offer price, else the list price).
/// </summary>
public record AssignPlanPaymentLinkRequest
{
    public Guid SubscriptionPlanId { get; init; }
    public decimal Amount { get; init; }
    public string? AmountReason { get; init; }
    public ClinicContactRequest Contact { get; init; } = new();

    public Guid AdminUserId { get; init; }
    public string AdminEmail { get; init; } = string.Empty;
}
