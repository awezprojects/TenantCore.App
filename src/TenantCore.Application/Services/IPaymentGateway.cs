namespace TenantCore.Application.Services;

/// <summary>Outcome of a gateway call, classified so the caller knows whether to retry.</summary>
public sealed record GatewayResult(bool Success, bool IsTransient, string? ErrorMessage)
{
    public static GatewayResult Ok() => new(true, false, null);
    public static GatewayResult Transient(string error) => new(false, true, error);
    public static GatewayResult Permanent(string error) => new(false, false, error);
}

/// <summary>Razorpay payment link fields the workflow needs — nothing more.</summary>
public sealed record PaymentLinkInfo(
    string Id,
    string ShortUrl,
    string Status,
    string? PaymentId,
    string? Method,
    decimal? AmountPaid,
    DateTime? ExpireBy);

/// <summary>
/// Port for the payment gateway (Razorpay). Implemented in Infrastructure. Every call is
/// idempotent-safe: creating a link with a reference_id that already exists returns the
/// existing link instead of erroring, and re-fetching/re-cancelling an already-terminal link
/// is a no-op success rather than a failure.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>False when KeyId/KeySecret are not configured — callers must refuse to create payments rather than call out.</summary>
    bool IsConfigured { get; }

    Task<(GatewayResult Result, PaymentLinkInfo? Link)> CreatePaymentLinkAsync(
        string referenceId, long amountInMinorUnits, string currency, string description,
        string customerName, string customerEmail, string? customerPhone,
        DateTime expireBy, string callbackUrl, CancellationToken ct = default);

    Task<(GatewayResult Result, PaymentLinkInfo? Link)> GetPaymentLinkByIdAsync(string gatewayLinkId, CancellationToken ct = default);

    /// <summary>Used when CreatePaymentLinkAsync's response was lost but the link was actually created — adopts the existing link by its reference_id.</summary>
    Task<(GatewayResult Result, PaymentLinkInfo? Link)> FindPaymentLinkByReferenceAsync(string referenceId, CancellationToken ct = default);

    /// <summary>Already cancelled/expired/paid all count as success — cancellation is idempotent.</summary>
    Task<GatewayResult> CancelPaymentLinkAsync(string gatewayLinkId, CancellationToken ct = default);

    /// <summary>Fixed-time HMAC-SHA256 comparison of the raw webhook body against WebhookSecret.</summary>
    bool VerifyWebhookSignature(string rawBody, string signature);
}
