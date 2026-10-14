namespace TenantCore.Shared.Dtos.UsageBilling;

/// <summary>
/// The wallet screen's single read: balance, threshold and the newest ledger page.
/// </summary>
public record ClinicWalletDto
{
    public decimal Balance { get; init; }
    public decimal LowBalanceThreshold { get; init; }

    /// <summary>At or below the threshold. A warning only — an empty wallet never locks the app.</summary>
    public bool IsLow { get; init; }

    public string Currency { get; init; } = "INR";
    public DateTime? LastTopUpAt { get; init; }

    /// <summary>Newest first, capped server-side.</summary>
    public IReadOnlyList<WalletTransactionDto> Transactions { get; init; } = [];
}
