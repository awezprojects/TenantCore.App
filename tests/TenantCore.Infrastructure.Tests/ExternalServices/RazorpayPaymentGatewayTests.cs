using System.Net;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using TenantCore.Application.Services;
using TenantCore.Infrastructure.ExternalServices.Razorpay;

namespace TenantCore.Infrastructure.Tests.ExternalServices;

/// <summary>
/// Tests for <see cref="RazorpayPaymentGateway"/>: webhook HMAC verification (accept/tamper),
/// HTTP status-code-to-classification mapping, and duplicate-reference detection on create —
/// all driven through a fake <see cref="HttpMessageHandler"/> rather than a live Razorpay call.
/// See plan/clinic-trial-razorpay-subscriptions/PLAN.md "Reliability Design" and building blocks.
/// </summary>
public class RazorpayPaymentGatewayTests
{
    private const string WebhookSecret = "whsec_test_secret_value";

    // ── Webhook signature verification ──────────────────────────────────────

    [Fact]
    public void VerifyWebhookSignature_CorrectlySignedPayload_IsAccepted()
    {
        var gateway = CreateGateway(HttpStatusCode.OK, "{}", out _);
        const string rawBody = "{\"event\":\"payment_link.paid\",\"payload\":{}}";
        var signature = ComputeHmac(rawBody, WebhookSecret);

        gateway.VerifyWebhookSignature(rawBody, signature).Should().BeTrue();
    }

    [Fact]
    public void VerifyWebhookSignature_TamperedBody_WithOriginalSignature_IsRejected()
    {
        var gateway = CreateGateway(HttpStatusCode.OK, "{}", out _);
        const string originalBody = "{\"event\":\"payment_link.paid\",\"payload\":{\"amount\":1000}}";
        const string tamperedBody = "{\"event\":\"payment_link.paid\",\"payload\":{\"amount\":9999999}}";
        var signature = ComputeHmac(originalBody, WebhookSecret);

        gateway.VerifyWebhookSignature(tamperedBody, signature).Should().BeFalse();
    }

    [Fact]
    public void VerifyWebhookSignature_TamperedSignature_IsRejected()
    {
        var gateway = CreateGateway(HttpStatusCode.OK, "{}", out _);
        const string rawBody = "{\"event\":\"payment_link.paid\"}";
        var correctSignature = ComputeHmac(rawBody, WebhookSecret);
        var tamperedSignature = correctSignature[..^2] + (correctSignature[^2..] == "00" ? "ff" : "00");

        gateway.VerifyWebhookSignature(rawBody, tamperedSignature).Should().BeFalse();
    }

    [Fact]
    public void VerifyWebhookSignature_WrongSecret_IsRejected()
    {
        var gateway = CreateGateway(HttpStatusCode.OK, "{}", out _);
        const string rawBody = "{\"event\":\"payment_link.paid\"}";
        var signature = ComputeHmac(rawBody, "a-completely-different-secret");

        gateway.VerifyWebhookSignature(rawBody, signature).Should().BeFalse();
    }

    [Fact]
    public void VerifyWebhookSignature_NoWebhookSecretConfigured_IsRejected()
    {
        var gateway = CreateGateway(HttpStatusCode.OK, "{}", out _, webhookSecret: "");
        const string rawBody = "{\"event\":\"payment_link.paid\"}";

        gateway.VerifyWebhookSignature(rawBody, "anything").Should().BeFalse();
    }

    [Fact]
    public void VerifyWebhookSignature_EmptySignature_IsRejected()
    {
        var gateway = CreateGateway(HttpStatusCode.OK, "{}", out _);

        gateway.VerifyWebhookSignature("{\"event\":\"x\"}", "").Should().BeFalse();
    }

    // ── Status-code → classification mapping ────────────────────────────────

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout, true)]      // 408
    [InlineData(HttpStatusCode.TooManyRequests, true)]     // 429
    [InlineData(HttpStatusCode.InternalServerError, true)] // 500
    [InlineData(HttpStatusCode.BadGateway, true)]          // 502
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]  // 503
    [InlineData(HttpStatusCode.BadRequest, false)]         // 400
    [InlineData(HttpStatusCode.Unauthorized, false)]       // 401
    [InlineData(HttpStatusCode.Forbidden, false)]          // 403
    [InlineData(HttpStatusCode.NotFound, false)]           // 404
    [InlineData(HttpStatusCode.Conflict, false)]           // 409
    public async Task CreatePaymentLinkAsync_ClassifiesHttpFailureByStatusCode(HttpStatusCode statusCode, bool expectedTransient)
    {
        const string body = "{\"error\":{\"code\":\"BAD_REQUEST_ERROR\",\"description\":\"something went wrong\"}}";
        var gateway = CreateGateway(statusCode, body, out _);

        var (result, link) = await gateway.CreatePaymentLinkAsync(
            "sp_reference", 10000, "INR", "Onboarding payment",
            "Dr Test", "doctor@test.example", "9999999999",
            DateTime.UtcNow.AddDays(7), "https://app.test/clinic-requests");

        result.Success.Should().BeFalse();
        result.IsTransient.Should().Be(expectedTransient);
        link.Should().BeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout, true)]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.NotFound, false)]
    public async Task GetPaymentLinkByIdAsync_ClassifiesHttpFailureByStatusCode(HttpStatusCode statusCode, bool expectedTransient)
    {
        const string body = "{\"error\":{\"description\":\"lookup failed\"}}";
        var gateway = CreateGateway(statusCode, body, out _);

        var (result, link) = await gateway.GetPaymentLinkByIdAsync("plink_123");

        result.Success.Should().BeFalse();
        result.IsTransient.Should().Be(expectedTransient);
        link.Should().BeNull();
    }

    [Fact]
    public async Task CreatePaymentLinkAsync_SuccessResponse_ReturnsOkAndMapsLinkFields()
    {
        const string body = """
        {
            "id": "plink_ABC123",
            "short_url": "https://rzp.io/i/abc123",
            "status": "created",
            "amount": 500000,
            "amount_paid": 0,
            "reference_id": "sp_reference"
        }
        """;
        var gateway = CreateGateway(HttpStatusCode.OK, body, out _);

        var (result, link) = await gateway.CreatePaymentLinkAsync(
            "sp_reference", 500000, "INR", "Onboarding payment",
            "Dr Test", "doctor@test.example", "9999999999",
            DateTime.UtcNow.AddDays(7), "https://app.test/clinic-requests");

        result.Success.Should().BeTrue();
        link.Should().NotBeNull();
        link!.Id.Should().Be("plink_ABC123");
        link.ShortUrl.Should().Be("https://rzp.io/i/abc123");
        link.Status.Should().Be("created");
    }

    [Fact]
    public async Task NotConfigured_MissingKeys_ReturnsPermanentWithoutCallingHttp()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://api.razorpay.test/v1/") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("RazorpayApi")).Returns(httpClient);

        var options = Microsoft.Extensions.Options.Options.Create(new RazorpayOptions { KeyId = "", KeySecret = "" });
        var gateway = new RazorpayPaymentGateway(factory.Object, options, NullLogger<RazorpayPaymentGateway>.Instance);

        gateway.IsConfigured.Should().BeFalse();

        var (result, link) = await gateway.CreatePaymentLinkAsync(
            "sp_reference", 1000, "INR", "desc", "Dr Test", "doctor@test.example", null,
            DateTime.UtcNow.AddDays(7), "https://app.test");

        result.Success.Should().BeFalse();
        result.IsTransient.Should().BeFalse();
        link.Should().BeNull();
        handler.Protected().Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    // ── Duplicate-reference detection on create ─────────────────────────────

    [Fact]
    public async Task CreatePaymentLinkAsync_DuplicateReferenceIdError_ReturnsPermanentDuplicateReferenceMarker()
    {
        const string body = """
        {
            "error": {
                "code": "BAD_REQUEST_ERROR",
                "description": "A link has already been created for this reference_id.",
                "reason": "input_duplicate"
            }
        }
        """;
        var gateway = CreateGateway(HttpStatusCode.BadRequest, body, out _);

        var (result, link) = await gateway.CreatePaymentLinkAsync(
            "sp_duplicate_reference", 10000, "INR", "Onboarding payment",
            "Dr Test", "doctor@test.example", "9999999999",
            DateTime.UtcNow.AddDays(7), "https://app.test/clinic-requests");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("DUPLICATE_REFERENCE");
        link.Should().BeNull();
    }

    [Fact]
    public async Task CreatePaymentLinkAsync_NonDuplicateBadRequest_DoesNotReturnDuplicateMarker()
    {
        const string body = """
        {
            "error": {
                "code": "BAD_REQUEST_ERROR",
                "description": "The amount must be at least 100 paise."
            }
        }
        """;
        var gateway = CreateGateway(HttpStatusCode.BadRequest, body, out _);

        var (result, _) = await gateway.CreatePaymentLinkAsync(
            "sp_reference", 10, "INR", "Onboarding payment",
            "Dr Test", "doctor@test.example", "9999999999",
            DateTime.UtcNow.AddDays(7), "https://app.test/clinic-requests");

        result.ErrorMessage.Should().NotBe("DUPLICATE_REFERENCE");
        result.IsTransient.Should().BeFalse();
    }

    [Fact]
    public async Task FindPaymentLinkByReferenceAsync_MatchingReference_AdoptsTheExistingLink()
    {
        const string body = """
        {
            "items": [
                { "id": "plink_OTHER", "short_url": "https://rzp.io/i/other", "status": "created", "amount": 1000, "amount_paid": 0, "reference_id": "sp_other" },
                { "id": "plink_MATCH", "short_url": "https://rzp.io/i/match", "status": "created", "amount": 5000, "amount_paid": 0, "reference_id": "sp_target" }
            ]
        }
        """;
        var gateway = CreateGateway(HttpStatusCode.OK, body, out _);

        var (result, link) = await gateway.FindPaymentLinkByReferenceAsync("sp_target");

        result.Success.Should().BeTrue();
        link.Should().NotBeNull();
        link!.Id.Should().Be("plink_MATCH");
    }

    [Fact]
    public async Task FindPaymentLinkByReferenceAsync_NoMatch_ReturnsPermanentNotFound()
    {
        const string body = """{ "items": [] }""";
        var gateway = CreateGateway(HttpStatusCode.OK, body, out _);

        var (result, link) = await gateway.FindPaymentLinkByReferenceAsync("sp_missing");

        result.Success.Should().BeFalse();
        result.IsTransient.Should().BeFalse();
        link.Should().BeNull();
    }

    [Fact]
    public async Task CancelPaymentLinkAsync_AlreadyPaidOrCancelledError_IsTreatedAsSuccess_Idempotent()
    {
        const string body = """{ "error": { "description": "The payment link has already been paid." } }""";
        var gateway = CreateGateway(HttpStatusCode.BadRequest, body, out _);

        var result = await gateway.CancelPaymentLinkAsync("plink_already_paid");

        result.Success.Should().BeTrue();
    }

    // ── Test helpers ─────────────────────────────────────────────────────────

    private static RazorpayPaymentGateway CreateGateway(
        HttpStatusCode statusCode, string responseBody, out Mock<HttpMessageHandler> handlerMock, string webhookSecret = WebhookSecret)
    {
        handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://api.razorpay.test/v1/") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("RazorpayApi")).Returns(httpClient);

        var options = Microsoft.Extensions.Options.Options.Create(new RazorpayOptions
        {
            KeyId = "rzp_test_key",
            KeySecret = "test_secret",
            WebhookSecret = webhookSecret,
            ApiBaseUrl = "https://api.razorpay.test/v1/"
        });

        return new RazorpayPaymentGateway(factory.Object, options, NullLogger<RazorpayPaymentGateway>.Instance);
    }

    private static string ComputeHmac(string rawBody, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var bodyBytes = Encoding.UTF8.GetBytes(rawBody);
        var hash = HMACSHA256.HashData(keyBytes, bodyBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
