using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TenantCore.Api.Controllers;

/// <summary>
/// Proxies authentication requests to the dedicated TenantCore.Auth service.
///
/// This keeps the Blazor WebAssembly client talking to a single API origin
/// (this application API), while the API forwards to the Auth service.
/// </summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController(IHttpClientFactory httpClientFactory) : ControllerBase
{
    private readonly HttpClient _authClient = httpClientFactory.CreateClient("AuthApi");

    [HttpPost("register")]
    public Task<IActionResult> RegisterAsync() => ForwardAsync(HttpMethod.Post, "api/auth/register");

    [HttpPost("login")]
    public Task<IActionResult> LoginAsync() => ForwardAsync(HttpMethod.Post, "api/auth/login");

    [HttpPost("2fa/validate-login")]
    public Task<IActionResult> ValidateTwoFactorAsync() => ForwardAsync(HttpMethod.Post, "api/auth/2fa/validate-login");

    [HttpPost("verify-email")]
    public Task<IActionResult> VerifyEmailAsync() => ForwardAsync(HttpMethod.Post, "api/auth/verify-email");

    [HttpPost("resend-email-verification")]
    public Task<IActionResult> ResendEmailVerificationAsync() => ForwardAsync(HttpMethod.Post, "api/auth/resend-email-verification");

    [HttpPost("reset-password")]
    public Task<IActionResult> ResetPasswordAsync() => ForwardAsync(HttpMethod.Post, "api/auth/reset-password");

    [HttpPost("accept-invitation")]
    public Task<IActionResult> AcceptInvitationAsync() => ForwardAsync(HttpMethod.Post, "api/auth/accept-invitation");

    [HttpGet("user/{userId:guid}")]
    public Task<IActionResult> GetUserByIdAsync(Guid userId) => ForwardAsync(HttpMethod.Get, $"api/auth/user/{userId}", includeBody: false);

    [HttpPost("refresh")]
    public Task<IActionResult> RefreshAsync() => ForwardAsync(HttpMethod.Post, "api/auth/refresh", includeBody: false);

    [HttpPost("logout")]
    public Task<IActionResult> LogoutAsync() => ForwardAsync(HttpMethod.Post, "api/auth/logout", includeBody: false);

    [HttpPost("logout-all")]
    public Task<IActionResult> LogoutAllAsync() => ForwardAsync(HttpMethod.Post, "api/auth/logout-all", includeBody: false);

    [HttpPost("2fa/disable/{userId:guid}")]
    public Task<IActionResult> DisableTwoFactorAsync(Guid userId) => ForwardAsync(HttpMethod.Post, $"api/auth/2fa/disable/{userId}");

    [HttpPost("forgot-password")]
    public Task<IActionResult> ForgotPasswordAsync() => ForwardAsync(HttpMethod.Post, "api/auth/forgot-password");

    [HttpPost("change-password/{userId:guid}")]
    public Task<IActionResult> ChangePasswordAsync(Guid userId) => ForwardAsync(HttpMethod.Post, $"api/auth/change-password/{userId}");

    [HttpGet("verify-email")]
    public Task<IActionResult> VerifyEmailGetAsync() => ForwardAsync(HttpMethod.Get, "api/auth/verify-email", includeBody: false);

    [HttpPost("2fa/enable/{userId:guid}")]
    public Task<IActionResult> EnableTwoFactorAsync(Guid userId) => ForwardAsync(HttpMethod.Post, $"api/auth/2fa/enable/{userId}", includeBody: false);

    [HttpPost("2fa/confirm/{userId:guid}")]
    public Task<IActionResult> ConfirmTwoFactorAsync(Guid userId) => ForwardAsync(HttpMethod.Post, $"api/auth/2fa/confirm/{userId}");

    [HttpPut("user/{userId:guid}/profile")]
    public Task<IActionResult> UpdateUserProfileAsync(Guid userId) => ForwardAsync(HttpMethod.Put, $"api/auth/user/{userId}/profile");

    [HttpPatch("user/{userId:guid}/activate")]
    public Task<IActionResult> ActivateUserAsync(Guid userId) => ForwardAsync(HttpMethod.Patch, $"api/auth/user/{userId}/activate", includeBody: false);

    [HttpPatch("user/{userId:guid}/deactivate")]
    public Task<IActionResult> DeactivateUserAsync(Guid userId) => ForwardAsync(HttpMethod.Patch, $"api/auth/user/{userId}/deactivate", includeBody: false);

    [HttpGet("user/search")]
    public Task<IActionResult> SearchUsersByEmailAsync() => ForwardAsync(HttpMethod.Get, "api/auth/user/search", includeBody: false);

    private async Task<IActionResult> ForwardAsync(HttpMethod method, string downstreamPath, bool includeBody = true)
    {
        var cancellationToken = HttpContext.RequestAborted;

        var downstreamUri = downstreamPath + Request.QueryString.Value;

        using var requestMessage = new HttpRequestMessage(method, downstreamUri);

        CopyHeaderIfPresent(requestMessage, "Authorization");
        CopyHeaderIfPresent(requestMessage, "X-ClinicApp-Id");
        CopyHeaderIfPresent(requestMessage, "X-Correlation-Id");

        if (includeBody && (Request.ContentLength is null || Request.ContentLength > 0))
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync(cancellationToken);

            var mediaType = "application/json";
            if (!string.IsNullOrWhiteSpace(Request.ContentType))
            {
                mediaType = Request.ContentType.Split(';', 2)[0].Trim();
            }

            requestMessage.Content = new StringContent(body, Encoding.UTF8, mediaType);
        }

        using var responseMessage = await _authClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (responseMessage.Headers.TryGetValues("Set-Cookie", out var setCookieValues))
        {
            foreach (var setCookie in setCookieValues)
            {
                Response.Headers.Append("Set-Cookie", setCookie);
            }
        }

        var responseBody = responseMessage.Content is null
            ? string.Empty
            : await responseMessage.Content.ReadAsStringAsync(cancellationToken);

        var responseContentType = responseMessage.Content?.Headers.ContentType?.ToString() ?? "application/json";

        return new ContentResult
        {
            StatusCode = (int)responseMessage.StatusCode,
            ContentType = responseContentType,
            Content = responseBody
        };
    }

    private void CopyHeaderIfPresent(HttpRequestMessage downstreamRequest, string headerName)
    {
        if (Request.Headers.TryGetValue(headerName, out var values))
        {
            downstreamRequest.Headers.TryAddWithoutValidation(headerName, values.ToArray());
        }
    }
}
