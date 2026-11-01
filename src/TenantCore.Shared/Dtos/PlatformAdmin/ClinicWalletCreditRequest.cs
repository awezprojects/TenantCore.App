namespace TenantCore.Shared.Dtos.PlatformAdmin;

/// <summary>
/// Internal — credits a clinic's wallet directly, off-gateway (a goodwill credit, a bank transfer
/// settled by hand). <see cref="ReferenceId"/> is <b>required</b> and is the idempotency key: the
/// ledger's unique index over (clinic, reference type, reference id) makes a retried call unable to
/// credit twice.
/// </summary>
public record ClinicWalletCreditRequest
{
    public decimal Amount { get; init; }

    public string Reason { get; init; } = string.Empty;

    /// <summary>Mandatory idempotency key for the credit.</summary>
    public Guid ReferenceId { get; init; }

    public Guid AdminUserId { get; init; }
    public string AdminEmail { get; init; } = string.Empty;
}
