namespace TenantCore.Infrastructure.ExternalServices.Razorpay;

/// <summary>Bound from the "Razorpay" configuration section. Missing KeyId/KeySecret must never fail startup — see IsConfigured.</summary>
public sealed class RazorpayOptions
{
    public string KeyId { get; set; } = string.Empty;
    public string KeySecret { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = "https://api.razorpay.com/v1/";
    public string Currency { get; set; } = "INR";
    public int PaymentLinkExpiryDays { get; set; } = 7;
    public string CallbackUrl { get; set; } = string.Empty;
    public bool NotifyByEmail { get; set; } = true;
    public bool NotifyBySms { get; set; } = true;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(KeyId) && !string.IsNullOrWhiteSpace(KeySecret);
}
