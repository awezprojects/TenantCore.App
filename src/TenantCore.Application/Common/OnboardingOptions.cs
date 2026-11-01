namespace TenantCore.Application.Common;

/// <summary>Bound from the "Onboarding" configuration section.</summary>
public sealed class OnboardingOptions
{
    /// <summary>"Approval" (default) or "SelfServe" — reserved for a later phase.</summary>
    public string Mode { get; set; } = "Approval";
    public string OpsNotificationEmail { get; set; } = string.Empty;
    public decimal MinPaymentAmount { get; set; } = 1.00m;
    public decimal MaxPaymentAmount { get; set; } = 500000m;
}
