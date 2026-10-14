namespace TenantCore.Shared.Enums;

/// <summary>
/// What a wallet ledger row refers to. Drives the idempotency of every credit and
/// debit: the unique index on (ApplicationId, ReferenceType, ReferenceId) means a
/// replay can never move money twice.
///
/// Integer values are a contract with TenantCore.Admin.
/// </summary>
public enum WalletReferenceType
{
    /// <summary>A clinic top-up settled by Razorpay — ReferenceId is the SubscriptionPayment.Id.</summary>
    TopUpPayment = 1,

    /// <summary>A metered usage charge — ReferenceId is the ClinicUsageRecord.Id.</summary>
    Usage = 2,

    /// <summary>A direct admin credit — ReferenceId is the admin-supplied idempotency key.</summary>
    AdminCredit = 3,

    /// <summary>A manual correction — ReferenceId is free-form.</summary>
    Adjustment = 4
}
