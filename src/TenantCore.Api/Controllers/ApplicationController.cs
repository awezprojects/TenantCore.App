using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TenantCore.Api.Controllers;

/// <summary>
/// Proxies Application management requests to the dedicated TenantCore.Auth service.
///
/// This keeps the Blazor WebAssembly client talking to a single API origin
/// (this application API), while the API forwards to the Auth service.
/// All endpoints require a valid JWT Bearer token.
/// </summary>
[ApiController]
[Route("api/Application")]
[Authorize]
public class ApplicationController(IHttpClientFactory httpClientFactory) : ControllerBase
{
    private readonly HttpClient _authClient = httpClientFactory.CreateClient("AuthApi");

    // POST /api/Application?ownerId={guid}
    [HttpPost]
    public Task<IActionResult> CreateApplicationAsync() =>
        ForwardAsync(HttpMethod.Post, "api/Application");

    // PUT /api/Application/{applicationId}
    [HttpPut("{applicationId:guid}")]
    public Task<IActionResult> EditApplicationAsync(Guid applicationId) =>
        ForwardAsync(HttpMethod.Put, $"api/Application/{applicationId}");

    // GET /api/Application/{applicationId}
    [HttpGet("{applicationId:guid}")]
    public Task<IActionResult> GetApplicationByIdAsync(Guid applicationId) =>
        ForwardAsync(HttpMethod.Get, $"api/Application/{applicationId}", includeBody: false);

    // GET /api/Application/by-code/{code}
    [HttpGet("by-code/{code}")]
    public Task<IActionResult> GetApplicationByCodeAsync(string code) =>
        ForwardAsync(HttpMethod.Get, $"api/Application/by-code/{Uri.EscapeDataString(code)}", includeBody: false);

    // GET /api/Application/get-all
    [HttpGet("get-all")]
    public Task<IActionResult> GetAllApplicationsAsync() =>
        ForwardAsync(HttpMethod.Get, "api/Application/get-all", includeBody: false);

    // GET /api/Application/by-type/{applicationType}
    [HttpGet("by-type/{applicationType:int}")]
    public Task<IActionResult> GetApplicationsByTypeAsync(int applicationType) =>
        ForwardAsync(HttpMethod.Get, $"api/Application/by-type/{applicationType}", includeBody: false);

    // GET /api/Application/{applicationId}/users
    [HttpGet("{applicationId:guid}/users")]
    public Task<IActionResult> GetApplicationUsersAsync(Guid applicationId) =>
        ForwardAsync(HttpMethod.Get, $"api/Application/{applicationId}/users", includeBody: false);

    // POST /api/Application/invite-user?invitedBy={guid}
    [HttpPost("invite-user")]
    public Task<IActionResult> InviteUserAsync() =>
        ForwardAsync(HttpMethod.Post, "api/Application/invite-user");

    // POST /api/Application/{applicationId}/users/{userId}/assign?roleId={guid}&assignedBy={guid}
    [HttpPost("{applicationId:guid}/users/{userId:guid}/assign")]
    public Task<IActionResult> AssignUserToApplicationAsync(Guid applicationId, Guid userId) =>
        ForwardAsync(HttpMethod.Post, $"api/Application/{applicationId}/users/{userId}/assign", includeBody: false);

    // POST /api/Application/{applicationId}/users/{userId}/mapping?assignedBy={guid}
    [HttpPost("{applicationId:guid}/users/{userId:guid}/mapping")]
    public Task<IActionResult> AddApplicationUserMappingAsync(Guid applicationId, Guid userId) =>
        ForwardAsync(HttpMethod.Post, $"api/Application/{applicationId}/users/{userId}/mapping", includeBody: false);

    // DELETE /api/Application/{applicationId}/users/{userId}?removedBy={guid}
    [HttpDelete("{applicationId:guid}/users/{userId:guid}")]
    public Task<IActionResult> RemoveUserFromApplicationAsync(Guid applicationId, Guid userId) =>
        ForwardAsync(HttpMethod.Delete, $"api/Application/{applicationId}/users/{userId}", includeBody: false);

    // DELETE /api/Application/{applicationId}
    [HttpDelete("{applicationId:guid}")]
    public Task<IActionResult> DeleteApplicationAsync(Guid applicationId) =>
        ForwardAsync(HttpMethod.Delete, $"api/Application/{applicationId}", includeBody: false);

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
