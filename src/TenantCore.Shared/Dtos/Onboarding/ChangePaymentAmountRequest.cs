namespace TenantCore.Shared.Dtos.Onboarding;

/// <summary>Internal — cancels the current unpaid link and issues a replacement for the new amount.</summary>
public record ChangePaymentAmountRequest
{
    public decimal Amount { get; init; }
    public string Reason { get; init; } = string.Empty;
    public Guid AdminUserId { get; init; }
    public string AdminEmail { get; init; } = string.Empty;
}
