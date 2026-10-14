namespace TenantCore.Shared.Enums;

/// <summary>
/// How a clinic pays for the platform: a package subscription, or metered pay-as-you-go
/// against a prepaid wallet.
///
/// Integer values are a contract with TenantCore.Admin (Core/Enums/BillingMode.cs).
/// </summary>
public enum BillingMode
{
    /// <summary>Default. The clinic holds a subscription term and is metered free for analytics.</summary>
    Subscription = 1,

    /// <summary>No subscription — each metered operation is debited from the clinic's wallet.</summary>
    PayAsYouGo = 2
}
