using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.JSInterop;
using Moq;
using TenantCore.Web.Client.Clients;
using TenantCore.Web.Client.Services;

namespace TenantCore.Web.Client.Tests.Clients;

public class ClinicApiClientTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static AuthStateService CreateAuthState(string? token = null)
    {
        var mockJs = new Mock<IJSRuntime>();
        var storage = new TokenStorageService(mockJs.Object);
        var authState = new AuthStateService(storage);

        if (token is not null)
        {
            typeof(AuthStateService)
                .GetField("_accessToken", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(authState, token);
        }

        return authState;
    }

    private static HttpClient CreateHttpClient(HttpResponseMessage response)
    {
        var handler = new StubHttpMessageHandler(response);
        return new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
    }

    private static HttpClient CreateFailingHttpClient()
    {
        var handler = new FailingHttpMessageHandler();
        return new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
    }

    private sealed class StubHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(response);
    }

    private sealed class FailingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("Network unreachable");
    }

    // ── SetAuth / Authorization header ───────────────────────────────────────

    [Fact]
    public async Task GetPatientByIdAsync_WhenTokenPresent_SetsAuthorizationHeader()
    {
        var capturedRequest = (HttpRequestMessage?)null;
        var handler = new CapturingHttpMessageHandler(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"id\":\"" + Guid.Empty + "\"}", Encoding.UTF8, "application/json"),
            };
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var authState = CreateAuthState("my-bearer-token");
        var client = new ClinicApiClient(httpClient, authState);

        await client.GetPatientByIdAsync(Guid.NewGuid());

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Authorization.Should().NotBeNull();
        capturedRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        capturedRequest.Headers.Authorization.Parameter.Should().Be("my-bearer-token");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetPatientByIdAsync_WhenTokenNullOrEmpty_DoesNotSetAuthorizationHeader(string? token)
    {
        var capturedRequest = (HttpRequestMessage?)null;
        var handler = new CapturingHttpMessageHandler(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"id\":\"" + Guid.Empty + "\"}", Encoding.UTF8, "application/json"),
            };
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var authState = CreateAuthState(token);
        var client = new ClinicApiClient(httpClient, authState);

        await client.GetPatientByIdAsync(Guid.NewGuid());

        capturedRequest!.Headers.Authorization.Should().BeNull();
    }

    private sealed class CapturingHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(respond(request));
    }

    // ── Ok<T> success path ───────────────────────────────────────────────────

    [Fact]
    public async Task GetPatientByIdAsync_WhenSuccessResponse_ReturnsSuccessWithDeserializedData()
    {
        var patientId = Guid.NewGuid();
        var json = $$"""{"id":"{{patientId}}","firstName":"John","lastName":"Doe","applicationId":"{{Guid.Empty}}","mrNumber":"","gender":0,"isActive":true,"createdAt":"2024-01-01T00:00:00Z","opdCount":0}""";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        var client = new ClinicApiClient(CreateHttpClient(response), CreateAuthState());

        var result = await client.GetPatientByIdAsync(patientId);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(patientId);
    }

    // ── Ok<T> failure path / ExtractUserMessage ───────────────────────────────

    [Fact]
    public async Task GetPatientByIdAsync_WhenNotFoundResponse_ReturnsFalseWithNotFoundMessage()
    {
        var json = """{"status":404,"title":"Not Found","detail":"The requested record was not found.","instance":"/api/patients/123"}""";
        var response = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        var client = new ClinicApiClient(CreateHttpClient(response), CreateAuthState());

        var result = await client.GetPatientByIdAsync(Guid.NewGuid());

        result.Success.Should().BeFalse();
        result.Message.Should().Be("The requested record was not found.");
    }

    [Fact]
    public async Task GetPatientByIdAsync_WhenBadRequestWithErrors_ReturnsJoinedValidationMessages()
    {
        var json = """{"status":400,"title":"Validation Error","errors":{"FirstName":["First name is required."],"LastName":["Last name is required."]}}""";
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        var client = new ClinicApiClient(CreateHttpClient(response), CreateAuthState());

        var result = await client.GetPatientByIdAsync(Guid.NewGuid());

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("First name is required.");
        result.Message.Should().Contain("Last name is required.");
    }

    [Fact]
    public async Task GetPatientByIdAsync_WhenNonJsonErrorBody_FallsBackToStatusCodeDefault()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("<html>Not Found</html>", Encoding.UTF8, "text/html"),
        };
        var client = new ClinicApiClient(CreateHttpClient(response), CreateAuthState());

        var result = await client.GetPatientByIdAsync(Guid.NewGuid());

        result.Success.Should().BeFalse();
        result.Message.Should().Be("The requested record was not found.");
    }

    [Fact]
    public async Task GetPatientByIdAsync_WhenEmptyErrorBody_FallsBackToStatusCodeDefault()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(string.Empty, Encoding.UTF8, "application/json"),
        };
        var client = new ClinicApiClient(CreateHttpClient(response), CreateAuthState());

        var result = await client.GetPatientByIdAsync(Guid.NewGuid());

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Please check your input and try again.");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "Your session has expired. Please log in again.")]
    [InlineData(HttpStatusCode.Forbidden, "You don't have permission to perform this action.")]
    [InlineData(HttpStatusCode.NotFound, "The requested record was not found.")]
    [InlineData(HttpStatusCode.Conflict, "This action conflicts with existing data.")]
    [InlineData(HttpStatusCode.InternalServerError, "A server error occurred. Please try again.")]
    public async Task GetPatientByIdAsync_StatusCodeDefaultMessages_ReturnExpectedMessage(
        HttpStatusCode statusCode, string expectedMessage)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(string.Empty, Encoding.UTF8, "application/json"),
        };
        var client = new ClinicApiClient(CreateHttpClient(response), CreateAuthState());

        var result = await client.GetPatientByIdAsync(Guid.NewGuid());

        result.Success.Should().BeFalse();
        result.Message.Should().Be(expectedMessage);
    }

    // ── Fail<T> / network error path ─────────────────────────────────────────

    [Fact]
    public async Task GetPatientByIdAsync_WhenNetworkException_ReturnsConnectionErrorMessage()
    {
        var client = new ClinicApiClient(CreateFailingHttpClient(), CreateAuthState());

        var result = await client.GetPatientByIdAsync(Guid.NewGuid());

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Could not connect to the server. Please check your connection and try again.");
    }

    [Fact]
    public async Task GetWardsAsync_WhenNetworkException_ReturnsConnectionErrorMessage()
    {
        var client = new ClinicApiClient(CreateFailingHttpClient(), CreateAuthState());

        var result = await client.GetWardsAsync();

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Could not connect to the server. Please check your connection and try again.");
    }
}
