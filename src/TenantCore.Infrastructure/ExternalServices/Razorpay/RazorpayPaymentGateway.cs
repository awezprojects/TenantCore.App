using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TenantCore.Application.Services;

namespace TenantCore.Infrastructure.ExternalServices.Razorpay;

/// <summary>
/// Razorpay Payment Links implementation of <see cref="IPaymentGateway"/>, over the named
/// "RazorpayApi" HttpClient (Basic auth KeyId:KeySecret, configured in DI). Never logs keys,
/// signatures or full response bodies — only status codes and Razorpay's own error codes.
/// </summary>
public sealed class RazorpayPaymentGateway(
    IHttpClientFactory httpClientFactory,
    IOptions<RazorpayOptions> options,
    ILogger<RazorpayPaymentGateway> logger) : IPaymentGateway
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string ClientName = "RazorpayApi";

    public bool IsConfigured => options.Value.IsConfigured;

    public async Task<(GatewayResult Result, PaymentLinkInfo? Link)> CreatePaymentLinkAsync(
        string referenceId, long amountInMinorUnits, string currency, string description,
        string customerName, string customerEmail, string? customerPhone,
        DateTime expireBy, string callbackUrl, CancellationToken ct = default)
    {
        if (!IsConfigured)
            return (GatewayResult.Permanent("Razorpay is not configured."), null);

        var request = new CreatePaymentLinkRequest
        {
            amount = amountInMinorUnits,
            currency = currency,
            description = description,
            customer = new RazorpayCustomer { name = customerName, email = customerEmail, contact = customerPhone },
            notify = new RazorpayNotify { sms = options.Value.NotifyBySms, email = options.Value.NotifyByEmail },
            callback_url = string.IsNullOrWhiteSpace(callbackUrl) ? null : callbackUrl,
            expire_by = ToUnixSeconds(expireBy),
            reference_id = referenceId
        };

        using var client = CreateClient();
        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync("payment_links", request, JsonOptions, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Razorpay create payment link — network/timeout error for reference {ReferenceId}", referenceId);
            return (GatewayResult.Transient(ex.Message), null);
        }

        if (response.IsSuccessStatusCode)
        {
            var link = await response.Content.ReadFromJsonAsync<PaymentLinkResponse>(JsonOptions, ct);
            return (GatewayResult.Ok(), ToInfo(link));
        }

        var error = await ReadErrorAsync(response, ct);

        // A duplicate reference_id is the exact race the caller needs to recover from —
        // surface it distinctly so the handler can adopt the existing link instead of failing.
        if (IsDuplicateReferenceError(error))
        {
            logger.LogInformation("Razorpay reports a payment link already exists for reference {ReferenceId} — caller should adopt it.", referenceId);
            return (GatewayResult.Permanent("DUPLICATE_REFERENCE"), null);
        }

        return (ClassifyFailure(response.StatusCode, error), null);
    }

    public async Task<(GatewayResult Result, PaymentLinkInfo? Link)> GetPaymentLinkByIdAsync(string gatewayLinkId, CancellationToken ct = default)
    {
        if (!IsConfigured)
            return (GatewayResult.Permanent("Razorpay is not configured."), null);

        using var client = CreateClient();
        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync($"payment_links/{Uri.EscapeDataString(gatewayLinkId)}", ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return (GatewayResult.Transient(ex.Message), null);
        }

        if (response.IsSuccessStatusCode)
        {
            var link = await response.Content.ReadFromJsonAsync<PaymentLinkResponse>(JsonOptions, ct);
            return (GatewayResult.Ok(), ToInfo(link));
        }

        var error = await ReadErrorAsync(response, ct);
        return (ClassifyFailure(response.StatusCode, error), null);
    }

    public async Task<(GatewayResult Result, PaymentLinkInfo? Link)> FindPaymentLinkByReferenceAsync(string referenceId, CancellationToken ct = default)
    {
        if (!IsConfigured)
            return (GatewayResult.Permanent("Razorpay is not configured."), null);

        using var client = CreateClient();
        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync($"payment_links?reference_id={Uri.EscapeDataString(referenceId)}", ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return (GatewayResult.Transient(ex.Message), null);
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await ReadErrorAsync(response, ct);
            return (ClassifyFailure(response.StatusCode, error), null);
        }

        var list = await response.Content.ReadFromJsonAsync<PaymentLinkListResponse>(JsonOptions, ct);
        var match = list?.items?.FirstOrDefault(i => i.reference_id == referenceId);
        if (match == null)
            return (GatewayResult.Permanent($"No payment link found for reference {referenceId}."), null);

        return (GatewayResult.Ok(), ToInfo(match));
    }

    public async Task<GatewayResult> CancelPaymentLinkAsync(string gatewayLinkId, CancellationToken ct = default)
    {
        if (!IsConfigured)
            return GatewayResult.Permanent("Razorpay is not configured.");

        using var client = CreateClient();
        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync($"payment_links/{Uri.EscapeDataString(gatewayLinkId)}/cancel", null, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return GatewayResult.Transient(ex.Message);
        }

        if (response.IsSuccessStatusCode)
            return GatewayResult.Ok();

        var error = await ReadErrorAsync(response, ct);

        // Cancelling an already-terminal (paid/cancelled/expired) link is treated as success —
        // cancellation is idempotent from the caller's point of view.
        if (IsAlreadyTerminalError(error))
            return GatewayResult.Ok();

        return ClassifyFailure(response.StatusCode, error);
    }

    public bool VerifyWebhookSignature(string rawBody, string signature)
    {
        if (string.IsNullOrEmpty(options.Value.WebhookSecret) || string.IsNullOrEmpty(signature))
            return false;

        var keyBytes = Encoding.UTF8.GetBytes(options.Value.WebhookSecret);
        var bodyBytes = Encoding.UTF8.GetBytes(rawBody);
        var expected = HMACSHA256.HashData(keyBytes, bodyBytes);
        var expectedHex = Convert.ToHexString(expected).ToLowerInvariant();

        var expectedBytes = Encoding.UTF8.GetBytes(expectedHex);
        var providedBytes = Encoding.UTF8.GetBytes(signature.Trim().ToLowerInvariant());
        if (expectedBytes.Length != providedBytes.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }

    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient(ClientName);
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{options.Value.KeyId}:{options.Value.KeySecret}"));
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
        return client;
    }

    private static PaymentLinkInfo? ToInfo(PaymentLinkResponse? link)
    {
        if (link == null)
            return null;

        var latestPayment = link.payments?.LastOrDefault(p => p.status == "captured")
                          ?? link.payments?.LastOrDefault();

        return new PaymentLinkInfo(
            Id: link.id,
            ShortUrl: link.short_url,
            Status: link.status,
            PaymentId: latestPayment?.payment_id,
            Method: latestPayment?.method,
            AmountPaid: link.amount_paid > 0 ? link.amount_paid / 100m : null,
            ExpireBy: link.expire_by.HasValue ? FromUnixSeconds(link.expire_by.Value) : null);
    }

    private static async Task<RazorpayErrorDetail?> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<RazorpayErrorResponse>(JsonOptions, ct);
            return body?.error;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool IsDuplicateReferenceError(RazorpayErrorDetail? error) =>
        error?.description?.Contains("reference", StringComparison.OrdinalIgnoreCase) == true &&
        error.description.Contains("already", StringComparison.OrdinalIgnoreCase);

    private static bool IsAlreadyTerminalError(RazorpayErrorDetail? error) =>
        error?.description != null && (
            error.description.Contains("already been cancelled", StringComparison.OrdinalIgnoreCase) ||
            error.description.Contains("cannot be cancelled", StringComparison.OrdinalIgnoreCase) ||
            error.description.Contains("expired", StringComparison.OrdinalIgnoreCase) ||
            error.description.Contains("paid", StringComparison.OrdinalIgnoreCase));

    private static GatewayResult ClassifyFailure(HttpStatusCode statusCode, RazorpayErrorDetail? error)
    {
        var message = error?.description ?? statusCode.ToString();
        var code = (int)statusCode;

        // 408/429 and every 5xx are transient — a retry can plausibly succeed.
        // Every other 4xx is permanent — retrying an invalid request never helps.
        var isTransient = code == 408 || code == 429 || code >= 500;
        return isTransient ? GatewayResult.Transient(message) : GatewayResult.Permanent(message);
    }

    private static long ToUnixSeconds(DateTime utc) => ((DateTimeOffset)DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToUnixTimeSeconds();

    private static DateTime FromUnixSeconds(long seconds) => DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
}
