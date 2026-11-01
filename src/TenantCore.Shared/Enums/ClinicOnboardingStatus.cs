namespace TenantCore.Shared.Enums;

public enum ClinicOnboardingStatus
{
    Submitted = 1,
    Approved = 2,
    AwaitingPayment = 3,
    PaymentReceived = 4,
    Provisioning = 5,
    Active = 6,
    Rejected = 7,
    Cancelled = 8,
    PaymentLinkExpired = 9
}
