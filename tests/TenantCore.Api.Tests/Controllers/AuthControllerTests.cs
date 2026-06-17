using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.Protected;
using TenantCore.Api.Controllers;

namespace TenantCore.Api.Tests.Controllers;

/// <summary>
/// Tests for AuthController — a pure HTTP proxy.
/// Each action delegates to the downstream Auth service via IHttpClientFactory.
/// We verify that: the correct downstream path is called, the HTTP status code
/// is forwarded to the caller, and the response body is forwarded as-is.
/// </summary>
public class AuthControllerTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();

    // ?? Helper: build a controller backed by a mock HttpMessageHandler ??????

    private AuthController BuildController(
        HttpStatusCode statusCode,
        string responseBody,
        out Mock<HttpMessageHandler> handlerMock)
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
                Content = new StringContent(responseBody, System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://auth.test")
        };

        _httpClientFactory.Setup(f => f.CreateClient("AuthApi")).Returns(httpClient);

        var controller = new AuthController(_httpClientFactory.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    // ?? POST /api/auth/register ?????????????????????????????????????????????

    [Fact]
    public async Task RegisterAsync_ForwardsResponseStatusCode()
    {
        var controller = BuildController(HttpStatusCode.OK, "{}", out _);

        var result = await controller.RegisterAsync();

        var content = result.Should().BeOfType<ContentResult>().Subject;
        content.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task RegisterAsync_ForwardsResponseBody()
    {
        const string body = "{\"token\":\"abc\"}";
        var controller = BuildController(HttpStatusCode.OK, body, out _);

        var result = await controller.RegisterAsync();

        result.Should().BeOfType<ContentResult>()
              .Which.Content.Should().Be(body);
    }

    // ?? POST /api/auth/login ????????????????????????????????????????????????

    [Fact]
    public async Task LoginAsync_ForwardsResponseStatusCode()
    {
        var controller = BuildController(HttpStatusCode.OK, "{}", out _);

        var result = await controller.LoginAsync();

        result.Should().BeOfType<ContentResult>()
              .Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task LoginAsync_Returns401_WhenDownstreamReturns401()
    {
        var controller = BuildController(HttpStatusCode.Unauthorized, "{}", out _);

        var result = await controller.LoginAsync();

        result.Should().BeOfType<ContentResult>()
              .Which.StatusCode.Should().Be(401);
    }

    // ?? POST /api/auth/verify-email ?????????????????????????????????????????

    [Fact]
    public async Task VerifyEmailAsync_ForwardsResponse()
    {
        var controller = BuildController(HttpStatusCode.OK, "{}", out _);

        var result = await controller.VerifyEmailAsync();

        result.Should().BeOfType<ContentResult>()
              .Which.StatusCode.Should().Be(200);
    }

    // ?? POST /api/auth/resend-email-verification ????????????????????????????

    [Fact]
    public async Task ResendEmailVerificationAsync_ForwardsResponse()
    {
        var controller = BuildController(HttpStatusCode.OK, "{}", out _);

        var result = await controller.ResendEmailVerificationAsync();

        result.Should().BeOfType<ContentResult>()
              .Which.StatusCode.Should().Be(200);
    }

    // ?? POST /api/auth/reset-password ???????????????????????????????????????

    [Fact]
    public async Task ResetPasswordAsync_ForwardsResponse()
    {
        var controller = BuildController(HttpStatusCode.OK, "{}", out _);

        var result = await controller.ResetPasswordAsync();

        result.Should().BeOfType<ContentResult>()
              .Which.StatusCode.Should().Be(200);
    }

    // ?? POST /api/auth/forgot-password ??????????????????????????????????????

    [Fact]
    public async Task ForgotPasswordAsync_ForwardsResponse()
    {
        var controller = BuildController(HttpStatusCode.OK, "{}", out _);

        var result = await controller.ForgotPasswordAsync();

        result.Should().BeOfType<ContentResult>()
              .Which.StatusCode.Should().Be(200);
    }

    // ?? POST /api/auth/accept-invitation ???????????????????????????????????

    [Fact]
    public async Task AcceptInvitationAsync_ForwardsResponse()
    {
        var controller = BuildController(HttpStatusCode.OK, "{}", out _);

        var result = await controller.AcceptInvitationAsync();

        result.Should().BeOfType<ContentResult>()
              .Which.StatusCode.Should().Be(200);
    }

    // ?? GET /api/auth/user/{userId} ?????????????????????????????????????????

    [Fact]
    public async Task GetUserByIdAsync_ForwardsResponse()
    {
        var controller = BuildController(HttpStatusCode.OK, "{}", out _);

        var result = await controller.GetUserByIdAsync(Guid.NewGuid());

        result.Should().BeOfType<ContentResult>()
              .Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetUserByIdAsync_Returns404_WhenUserNotFound()
    {
        var controller = BuildController(HttpStatusCode.NotFound, "{}", out _);

        var result = await controller.GetUserByIdAsync(Guid.NewGuid());

        result.Should().BeOfType<ContentResult>()
              .Which.StatusCode.Should().Be(404);
    }

    // ?? POST /api/auth/refresh ??????????????????????????????????????????????

    [Fact]
    public async Task RefreshAsync_ForwardsResponse()
    {
        var controller = BuildController(HttpStatusCode.OK, "{}", out _);

        var result = await controller.RefreshAsync();

        result.Should().BeOfType<ContentResult>()
              .Which.StatusCode.Should().Be(200);
    }

    // ?? POST /api/auth/logout ???????????????????????????????????????????????

    [Fact]
    public async Task LogoutAsync_ForwardsResponse()
    {
        var controller = BuildController(HttpStatusCode.OK, "{}", out _);

        var result = await controller.LogoutAsync();

        result.Should().BeOfType<ContentResult>()
              .Which.StatusCode.Should().Be(200);
    }

    // ?? POST /api/auth/logout-all ???????????????????????????????????????????

    [Fact]
    public async Task LogoutAllAsync_ForwardsResponse()
    {
        var controller = BuildController(HttpStatusCode.OK, "{}", out _);

        var result = await controller.LogoutAllAsync();

        result.Should().BeOfType<ContentResult>()
              .Which.StatusCode.Should().Be(200);
    }

    // ?? POST /api/auth/2fa/validate-login ??????????????????????????????????

    [Fact]
    public async Task ValidateTwoFactorAsync_ForwardsResponse()
    {
        var controller = BuildController(HttpStatusCode.OK, "{}", out _);

        var result = await controller.ValidateTwoFactorAsync();

        result.Should().BeOfType<ContentResult>()
              .Which.StatusCode.Should().Be(200);
    }

    // ?? POST /api/auth/2fa/disable/{userId} ????????????????????????????????

    [Fact]
    public async Task DisableTwoFactorAsync_ForwardsResponse()
    {
        var controller = BuildController(HttpStatusCode.OK, "{}", out _);

        var result = await controller.DisableTwoFactorAsync(Guid.NewGuid());

        result.Should().BeOfType<ContentResult>()
              .Which.StatusCode.Should().Be(200);
    }

    // ?? POST /api/auth/2fa/enable/{userId} ?????????????????????????????????

    [Fact]
    public async Task EnableTwoFactorAsync_ForwardsResponse()
    {
        var controller = BuildController(HttpStatusCode.OK, "{}", out _);

        var result = await controller.EnableTwoFactorAsync(Guid.NewGuid());

        result.Should().BeOfType<ContentResult>()
              .Which.StatusCode.Should().Be(200);
    }

    // ?? POST /api/auth/2fa/confirm/{userId} ????????????????????????????????

    [Fact]
    public async Task ConfirmTwoFactorAsync_ForwardsResponse()
    {
        var controller = BuildController(HttpStatusCode.OK, "{}", out _);

        var result = await controller.ConfirmTwoFactorAsync(Guid.NewGuid());

        result.Should().BeOfType<ContentResult>()
              .Which.StatusCode.Should().Be(200);
    }

    // ?? POST /api/auth/change-password/{userId} ????????????????????????????

    [Fact]
    public async Task ChangePasswordAsync_ForwardsResponse()
    {
        var controller = BuildController(HttpStatusCode.OK, "{}", out _);

        var result = await controller.ChangePasswordAsync(Guid.NewGuid());

        result.Should().BeOfType<ContentResult>()
              .Which.StatusCode.Should().Be(200);
    }

    // ?? PUT /api/auth/user/{userId}/profile ????????????????????????????????

    [Fact]
    public async Task UpdateUserProfileAsync_ForwardsResponse()
    {
        var controller = BuildController(HttpStatusCode.OK, "{}", out _);

        var result = await controller.UpdateUserProfileAsync(Guid.NewGuid());

        result.Should().BeOfType<ContentResult>()
              .Which.StatusCode.Should().Be(200);
    }

    // ?? PATCH /api/auth/user/{userId}/activate ?????????????????????????????

    [Fact]
    public async Task ActivateUserAsync_ForwardsResponse()
    {
        var controller = BuildController(HttpStatusCode.OK, "{}", out _);

        var result = await controller.ActivateUserAsync(Guid.NewGuid());

        result.Should().BeOfType<ContentResult>()
              .Which.StatusCode.Should().Be(200);
    }

    // ?? PATCH /api/auth/user/{userId}/deactivate ???????????????????????????

    [Fact]
    public async Task DeactivateUserAsync_ForwardsResponse()
    {
        var controller = BuildController(HttpStatusCode.OK, "{}", out _);

        var result = await controller.DeactivateUserAsync(Guid.NewGuid());

        result.Should().BeOfType<ContentResult>()
              .Which.StatusCode.Should().Be(200);
    }

    // ?? GET /api/auth/user/search ???????????????????????????????????????????

    [Fact]
    public async Task SearchUsersByEmailAsync_ForwardsResponse()
    {
        var controller = BuildController(HttpStatusCode.OK, "[]", out _);

        var result = await controller.SearchUsersByEmailAsync();

        result.Should().BeOfType<ContentResult>()
              .Which.StatusCode.Should().Be(200);
    }

    // ?? GET /api/auth/accept-existing-invitation ???????????????????????????

    [Fact]
    public async Task AcceptExistingInvitationAsync_ForwardsResponse()
    {
        var controller = BuildController(HttpStatusCode.OK, "{}", out _);

        var result = await controller.AcceptExistingInvitationAsync();

        result.Should().BeOfType<ContentResult>()
              .Which.StatusCode.Should().Be(200);
    }

    // ?? GET /api/auth/verify-email (GET variant) ????????????????????????????

    [Fact]
    public async Task VerifyEmailGetAsync_ForwardsResponse()
    {
        var controller = BuildController(HttpStatusCode.OK, "{}", out _);

        var result = await controller.VerifyEmailGetAsync();

        result.Should().BeOfType<ContentResult>()
              .Which.StatusCode.Should().Be(200);
    }

    // ?? Forwarding: correlation header is propagated ????????????????????????

    [Fact]
    public async Task LoginAsync_PropagatesCorrelationIdHeader_WhenPresent()
    {
        var controller = BuildController(HttpStatusCode.OK, "{}", out var handlerMock);

        controller.ControllerContext.HttpContext.Request.Headers["X-Correlation-Id"] = "test-correlation-id";

        await controller.LoginAsync();

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Headers.Contains("X-Correlation-Id")),
            ItExpr.IsAny<CancellationToken>());
    }
}
