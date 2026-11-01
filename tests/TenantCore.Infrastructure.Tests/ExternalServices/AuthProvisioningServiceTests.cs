using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using TenantCore.Application.Services;
using TenantCore.Infrastructure.ExternalServices;
using TenantCore.Infrastructure.Services;

namespace TenantCore.Infrastructure.Tests.ExternalServices;

/// <summary>
/// Tests for <see cref="AuthProvisioningService"/> — the App's client for Auth's internal,
/// service-key-protected clinic-provisioning endpoint. Runs from the background worker, which
/// has no HTTP request or user token: it must send X-Internal-Service-Key and must never send
/// an Authorization: Bearer header. Driven through a fake HttpMessageHandler.
/// </summary>
public class AuthProvisioningServiceTests
{
    private static readonly Guid ProvisioningReference = Guid.NewGuid();
    private static readonly Guid OwnerUserId = Guid.NewGuid();

    // ── Header shape: service key, never a bearer token ─────────────────────

    [Fact]
    public async Task ProvisionClinicAsync_SendsServiceKeyHeader_AndNeverSetsAuthorizationBearer()
    {
        HttpRequestMessage? capturedRequest = null;
        var applicationId = Guid.NewGuid();
        var responseBody = SuccessBody(applicationId);

        var service = CreateService(HttpStatusCode.OK, responseBody, out var handlerMock, serviceKey: "shared-service-key-123");
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            });

        await service.ProvisionClinicAsync(ProvisioningReference, OwnerUserId, "Test Clinic", "TESTCODE", null, null, null, null, null, null);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.TryGetValues("X-Internal-Service-Key", out var values).Should().BeTrue();
        values!.Single().Should().Be("shared-service-key-123");
        capturedRequest.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task ProvisionClinicAsync_CorrelationIdSetOnContext_ForwardsXCorrelationIdHeader()
    {
        var correlationId = Guid.NewGuid();
        HttpRequestMessage? capturedRequest = null;
        var responseBody = SuccessBody(Guid.NewGuid());

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://auth-internal.test") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("AuthInternalApi")).Returns(httpClient);
        var options = Options.Create(new AuthInternalApiOptions { ServiceKey = "shared-key" });
        var correlationContext = new WorkflowCorrelationContext { CorrelationId = correlationId };
        var service = new AuthProvisioningService(factory.Object, options, correlationContext, NullLogger<AuthProvisioningService>.Instance);

        await service.ProvisionClinicAsync(ProvisioningReference, OwnerUserId, "Test Clinic", "TESTCODE", null, null, null, null, null, null);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.TryGetValues("X-Correlation-Id", out var values).Should().BeTrue();
        values!.Single().Should().Be(correlationId.ToString());
    }

    [Fact]
    public async Task ProvisionClinicAsync_NoCorrelationIdOnContext_DoesNotSendCorrelationHeader()
    {
        HttpRequestMessage? capturedRequest = null;
        var service = CreateService(HttpStatusCode.OK, SuccessBody(Guid.NewGuid()), out var handlerMock);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(SuccessBody(Guid.NewGuid()), Encoding.UTF8, "application/json")
            });

        await service.ProvisionClinicAsync(ProvisioningReference, OwnerUserId, "Test Clinic", "TESTCODE", null, null, null, null, null, null);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Contains("X-Correlation-Id").Should().BeFalse();
    }

    [Fact]
    public async Task ProvisionClinicAsync_ServiceKeyNotConfigured_ReturnsPermanentWithoutCallingHttp()
    {
        var service = CreateService(HttpStatusCode.OK, "{}", out var handlerMock, serviceKey: "");

        var outcome = await service.ProvisionClinicAsync(ProvisioningReference, OwnerUserId, "Test Clinic", "TESTCODE", null, null, null, null, null, null);

        outcome.Kind.Should().Be(ProvisioningOutcomeKind.Permanent);
        handlerMock.Protected().Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    // ── Response classification ──────────────────────────────────────────────

    [Fact]
    public async Task ProvisionClinicAsync_SuccessResponse_ReturnsSuccessWithApplicationId()
    {
        var applicationId = Guid.NewGuid();
        var service = CreateService(HttpStatusCode.OK, SuccessBody(applicationId), out _);

        var outcome = await service.ProvisionClinicAsync(ProvisioningReference, OwnerUserId, "Test Clinic", "TESTCODE", null, null, null, null, null, null);

        outcome.Kind.Should().Be(ProvisioningOutcomeKind.Success);
        outcome.ApplicationId.Should().Be(applicationId);
    }

    [Fact]
    public async Task ProvisionClinicAsync_AlreadyExistedResponse_StillReturnsSuccess_Idempotent()
    {
        var applicationId = Guid.NewGuid();
        const string body = """
        {
            "success": true,
            "message": "Already provisioned",
            "data": { "applicationId": "REPLACE", "alreadyExisted": true }
        }
        """;
        var service = CreateService(HttpStatusCode.OK, body.Replace("REPLACE", applicationId.ToString()), out _);

        var outcome = await service.ProvisionClinicAsync(ProvisioningReference, OwnerUserId, "Test Clinic", "TESTCODE", null, null, null, null, null, null);

        outcome.Kind.Should().Be(ProvisioningOutcomeKind.Success);
        outcome.ApplicationId.Should().Be(applicationId);
    }

    [Theory]
    [InlineData("CodeTaken")]
    [InlineData("OwnerNotFound")]
    [InlineData("OwnerInactive")]
    [InlineData("RolesNotConfigured")]
    [InlineData("Invalid")]
    public async Task ProvisionClinicAsync_KnownPermanentErrorCode_ReturnsPermanent(string errorCode)
    {
        var body = $$"""
        {
            "success": false,
            "message": "Cannot provision",
            "data": { "applicationId": "00000000-0000-0000-0000-000000000000", "errorCode": "{{errorCode}}" }
        }
        """;
        var service = CreateService(HttpStatusCode.OK, body, out _);

        var outcome = await service.ProvisionClinicAsync(ProvisioningReference, OwnerUserId, "Test Clinic", "TESTCODE", null, null, null, null, null, null);

        outcome.Kind.Should().Be(ProvisioningOutcomeKind.Permanent);
        outcome.ApplicationId.Should().BeNull();
    }

    [Fact]
    public async Task ProvisionClinicAsync_UnrecognisedErrorCode_TreatedCautiouslyAsTransient()
    {
        const string body = """
        {
            "success": false,
            "message": "Unexpected",
            "data": { "applicationId": "00000000-0000-0000-0000-000000000000", "errorCode": "SomeNewCodeWeDontKnowAbout" }
        }
        """;
        var service = CreateService(HttpStatusCode.OK, body, out _);

        var outcome = await service.ProvisionClinicAsync(ProvisioningReference, OwnerUserId, "Test Clinic", "TESTCODE", null, null, null, null, null, null);

        outcome.Kind.Should().Be(ProvisioningOutcomeKind.Transient);
    }

    [Fact]
    public async Task ProvisionClinicAsync_Unauthorized_ReturnsPermanent_ServiceKeyMismatch()
    {
        var service = CreateService(HttpStatusCode.Unauthorized, "", out _);

        var outcome = await service.ProvisionClinicAsync(ProvisioningReference, OwnerUserId, "Test Clinic", "TESTCODE", null, null, null, null, null, null);

        outcome.Kind.Should().Be(ProvisioningOutcomeKind.Permanent);
        outcome.ErrorMessage.Should().Contain("service key");
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    public async Task ProvisionClinicAsync_ServerErrorOrTimeoutStatus_ReturnsTransient(HttpStatusCode statusCode)
    {
        var service = CreateService(statusCode, "", out _);

        var outcome = await service.ProvisionClinicAsync(ProvisioningReference, OwnerUserId, "Test Clinic", "TESTCODE", null, null, null, null, null, null);

        outcome.Kind.Should().Be(ProvisioningOutcomeKind.Transient);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task ProvisionClinicAsync_OtherNonSuccessStatus_ReturnsPermanent(HttpStatusCode statusCode)
    {
        var service = CreateService(statusCode, "", out _);

        var outcome = await service.ProvisionClinicAsync(ProvisioningReference, OwnerUserId, "Test Clinic", "TESTCODE", null, null, null, null, null, null);

        outcome.Kind.Should().Be(ProvisioningOutcomeKind.Permanent);
    }

    [Fact]
    public async Task ProvisionClinicAsync_NetworkFailure_ReturnsTransient()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("connection refused"));

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://auth-internal.test") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("AuthInternalApi")).Returns(httpClient);
        var options = Options.Create(new AuthInternalApiOptions { ServiceKey = "shared-key" });
        var service = new AuthProvisioningService(factory.Object, options, new WorkflowCorrelationContext(), NullLogger<AuthProvisioningService>.Instance);

        var outcome = await service.ProvisionClinicAsync(ProvisioningReference, OwnerUserId, "Test Clinic", "TESTCODE", null, null, null, null, null, null);

        outcome.Kind.Should().Be(ProvisioningOutcomeKind.Transient);
    }

    [Fact]
    public async Task ProvisionClinicAsync_UnparsableResponseBody_ReturnsTransient()
    {
        var service = CreateService(HttpStatusCode.OK, "not json at all", out _);

        var outcome = await service.ProvisionClinicAsync(ProvisioningReference, OwnerUserId, "Test Clinic", "TESTCODE", null, null, null, null, null, null);

        outcome.Kind.Should().Be(ProvisioningOutcomeKind.Transient);
    }

    // ── Test helpers ─────────────────────────────────────────────────────────

    private static string SuccessBody(Guid applicationId) => $$"""
    {
        "success": true,
        "message": "Provisioned",
        "data": {
            "applicationId": "{{applicationId}}",
            "clinicName": "Test Clinic",
            "clinicCode": "TESTCODE",
            "ownerUserId": "{{Guid.NewGuid()}}",
            "alreadyExisted": false
        }
    }
    """;

    private static AuthProvisioningService CreateService(
        HttpStatusCode statusCode, string responseBody, out Mock<HttpMessageHandler> handlerMock, string serviceKey = "shared-service-key")
    {
        handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://auth-internal.test") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("AuthInternalApi")).Returns(httpClient);

        var options = Options.Create(new AuthInternalApiOptions { ServiceKey = serviceKey, BaseUrl = "https://auth-internal.test" });

        return new AuthProvisioningService(factory.Object, options, new WorkflowCorrelationContext(), NullLogger<AuthProvisioningService>.Instance);
    }
}
