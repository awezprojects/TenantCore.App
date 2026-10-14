namespace TenantCore.Shared.Enums;

/// <summary>
/// Integer values are a contract with TenantCore.Admin (Core/Enums/PaymentPurpose.cs).
/// </summary>
public enum PaymentPurpose
{
    Onboarding = 1,
    Renewal = 2,

    /// <summary>A payment link an internal admin sent to an existing clinic. Activates exactly like a renewal.</summary>
    AdminAssigned = 3,

    /// <summary>
    /// A clinic-initiated top-up of its pay-as-you-go wallet. Carries no plan, so
    /// SubscriptionPayment.SubscriptionPlanId / PlanCode / PlanName are null on these rows.
    /// </summary>
    WalletTopUp = 4,

    /// <summary>
    /// The one-time, per-clinic registration fee. Collected on its own Razorpay link by the
    /// Clinic Admin and never ridden inside a plan payment (it must also work for a
    /// pay-as-you-go clinic, which has no plan payment at all).
    /// </summary>
    RegistrationFee = 5
}
