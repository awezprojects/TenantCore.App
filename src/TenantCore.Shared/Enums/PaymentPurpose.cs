namespace TenantCore.Shared.Enums;

/// <summary>
/// Integer values are a contract with TenantCore.Admin (Core/Enums/PaymentPurpose.cs).
/// </summary>
public enum PaymentPurpose
{
    Onboarding = 1,
    Renewal = 2,

    /// <summary>A payment link an internal admin sent to an existing clinic. Activates exactly like a renewal.</summary>
    AdminAssigned = 3
}
