using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/role")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class RoleController(IHttpClientFactory httpClientFactory) : ClinicControllerBase
{
    [HttpGet("by-application/{applicationId:guid}")]
    public async Task<IActionResult> GetRolesByApplication(Guid applicationId)
    {
        var contextApplicationId = GetApplicationId();
        if (contextApplicationId != applicationId)
            return Forbid();

        var client = httpClientFactory.CreateClient("AuthApi");
        var request = new HttpRequestMessage(HttpMethod.Get, $"api/role/by-application/{contextApplicationId}");
        request.Headers.Add("X-ClinicApp-Id", contextApplicationId.ToString());
        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            request.Headers.TryAddWithoutValidation("Authorization", (string)authHeader);

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
