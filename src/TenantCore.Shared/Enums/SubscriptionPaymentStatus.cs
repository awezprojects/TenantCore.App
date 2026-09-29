namespace TenantCore.Shared.Enums;

public enum SubscriptionPaymentStatus
{
    /// <summary>Row exists, link not yet created at Razorpay.</summary>
    Pending = 1,
    LinkCreated = 2,
    Paid = 3,
    Expired = 4,
    Cancelled = 5,

    /// <summary>Replaced by a later "change amount" — never the one the doctor should pay.</summary>
    Superseded = 6
}
