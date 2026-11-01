namespace TenantCore.Shared.Errors;

/// <summary>Stable error codes surfaced on ProblemDetails.Extensions["errorCode"] for onboarding/payment failures.</summary>
public static class OnboardingErrorCodes
{
    public const string OpenRequestExists = "OpenRequestExists";
    public const string SelfServiceCreationDisabled = "SelfServiceCreationDisabled";
    public const string PaidPlanRequiresPaymentLink = "PaidPlanRequiresPaymentLink";
    public const string PaymentsNotConfigured = "PaymentsNotConfigured";
}
