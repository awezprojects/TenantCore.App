namespace TenantCore.Shared.Dtos.PlatformAdmin;

/// <summary>
/// Internal — creates a Razorpay payment link that tops up the clinic's wallet, and emails it.
/// <see cref="ReferenceId"/> is the Admin portal's idempotency key for the request itself.
/// </summary>
public record ClinicWalletTopUpRequest
{
    public decimal Amount { get; init; }

    /// <summary>Why the admin is asking for the top-up. Shown in the admin audit trail only.</summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>Admin-supplied idempotency key, so a retried call cannot open a second link.</summary>
    public Guid? ReferenceId { get; init; }

    public Guid AdminUserId { get; init; }
    public string AdminEmail { get; init; } = string.Empty;
}
