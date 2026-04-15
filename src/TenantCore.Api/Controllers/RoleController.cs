using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Shared.Dtos.Auth;
using System.Net.Http;
using System.Net.Http.Json;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/role")]
[Authorize]
public class RoleController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    public RoleController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("by-application/{applicationId:guid}")]
    public async Task<IActionResult> GetRolesByApplication(Guid applicationId)
    {
        var client = _httpClientFactory.CreateClient("AuthApi");
        var request = new HttpRequestMessage(HttpMethod.Get, $"api/role/by-application/{applicationId}");
        request.Headers.Add("X-ClinicApp-Id", applicationId.ToString());
        // Forward the Authorization header from the incoming request
        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            request.Headers.Add("Authorization", (string)authHeader);
        }
        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        return new ContentResult
        {
            Content = content,
            StatusCode = (int)response.StatusCode,
            ContentType = "application/json"
        };
    }
}
