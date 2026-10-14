using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos.UsageBilling;

/// <summary>
/// One wallet ledger movement. <see cref="Amount"/> is always positive — <see cref="Type"/> carries
/// the sign — and <see cref="BalanceAfter"/> lets the ledger read top-to-bottom without replaying it.
/// </summary>
public record WalletTransactionDto
{
    public DateTime OccurredAt { get; init; }
    public WalletTransactionType Type { get; init; }
    public decimal Amount { get; init; }
    public decimal BalanceAfter { get; init; }
    public WalletReferenceType ReferenceType { get; init; }

    /// <summary>System-generated only — never a patient name or user free text.</summary>
    public string Description { get; init; } = string.Empty;
}
