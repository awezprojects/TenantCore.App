namespace TenantCore.Shared.Enums;

/// <summary>
/// The direction of a wallet ledger movement. <c>Amount</c> is always positive on the
/// entity — this enum carries the sign.
///
/// Integer values are a contract with TenantCore.Admin.
/// </summary>
public enum WalletTransactionType
{
    Credit = 1,
    Debit = 2
}
