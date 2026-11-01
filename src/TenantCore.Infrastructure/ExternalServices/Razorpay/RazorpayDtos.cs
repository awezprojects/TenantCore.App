namespace TenantCore.Infrastructure.ExternalServices.Razorpay;

// Infrastructure-only JSON shapes for Razorpay's Payment Links API. Field names follow
// Razorpay's documented snake_case API — verify against the live sandbox during setup,
// since this is written from documented behaviour rather than a live call in this environment.

internal sealed class RazorpayCustomer
{
    public string? name { get; set; }
    public string? email { get; set; }
    public string? contact { get; set; }
}

internal sealed class RazorpayNotify
{
    public bool sms { get; set; }
    public bool email { get; set; }
}

internal sealed class CreatePaymentLinkRequest
{
    public long amount { get; set; }
    public string currency { get; set; } = "INR";
    public bool accept_partial { get; set; }
    public string? description { get; set; }
    public RazorpayCustomer customer { get; set; } = new();
    public RazorpayNotify notify { get; set; } = new();
    public bool reminder_enable { get; set; } = true;
    public string? callback_url { get; set; }
    public string callback_method { get; set; } = "get";
    public long expire_by { get; set; }
    public string reference_id { get; set; } = string.Empty;
}

internal sealed class RazorpayPaymentLinkPaymentEntry
{
    public string? payment_id { get; set; }
    public string? status { get; set; }
    public string? method { get; set; }
    public long amount { get; set; }
}

internal sealed class PaymentLinkResponse
{
    public string id { get; set; } = string.Empty;
    public string short_url { get; set; } = string.Empty;

    /// <summary>created / partially_paid / paid / cancelled / expired.</summary>
    public string status { get; set; } = string.Empty;
    public long amount { get; set; }
    public long amount_paid { get; set; }
    public string? reference_id { get; set; }
    public long? expire_by { get; set; }
    public List<RazorpayPaymentLinkPaymentEntry>? payments { get; set; }
}

internal sealed class PaymentLinkListResponse
{
    public List<PaymentLinkResponse>? items { get; set; }
}

internal sealed class RazorpayErrorResponse
{
    public RazorpayErrorDetail? error { get; set; }
}

internal sealed class RazorpayErrorDetail
{
    public string? code { get; set; }
    public string? description { get; set; }
    public string? reason { get; set; }
}
