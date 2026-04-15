using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Services;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Infrastructure.ExternalServices;

/// <summary>
/// Implementation of <see cref="IAuthApplicationService"/> that forwards requests
/// to the TenantCore.Auth external API, propagating the caller's bearer token.
/// </summary>
public sealed class AuthApplicationService(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuthApplicationService> logger)
    : IAuthApplicationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("AuthApi");
        var token = httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrWhiteSpace(token))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
        }
        return client;
    }

    public async Task<ApplicationResponseDto> CreateApplicationAsync(Guid ownerId, ApplicationCreationRequestDto request, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        var response = await client.PostAsJsonAsync($"api/Application?ownerId={ownerId}", request, JsonOptions, cancellationToken);
        return await ParseRequiredAsync<ApplicationResponseDto>(response, cancellationToken);
    }

    public async Task<ApplicationResponseDto> EditApplicationAsync(Guid applicationId, ApplicationCreationRequestDto request, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        var response = await client.PutAsJsonAsync($"api/Application/{applicationId}", request, JsonOptions, cancellationToken);
        return await ParseRequiredAsync<ApplicationResponseDto>(response, cancellationToken);
    }

    public async Task<ApplicationResponseDto?> GetApplicationByIdAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        var response = await client.GetAsync($"api/Application/{applicationId}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        return await ParseRequiredAsync<ApplicationResponseDto>(response, cancellationToken);
    }

    public async Task<ApplicationResponseDto?> GetApplicationByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        var response = await client.GetAsync($"api/Application/by-code/{Uri.EscapeDataString(code)}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        return await ParseRequiredAsync<ApplicationResponseDto>(response, cancellationToken);
    }

    public async Task<List<ApplicationResponseDto>> GetAllApplicationsAsync(CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        var response = await client.GetAsync("api/Application/get-all", cancellationToken);
        return await ParseRequiredAsync<List<ApplicationResponseDto>>(response, cancellationToken);
    }

    public async Task<List<ApplicationResponseDto>> GetApplicationsByTypeAsync(int applicationType, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        var response = await client.GetAsync($"api/Application/by-type/{applicationType}", cancellationToken);
        return await ParseRequiredAsync<List<ApplicationResponseDto>>(response, cancellationToken);
    }

    public async Task<List<ApplicationUserResponseDto>> GetApplicationUsersAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        var response = await client.GetAsync($"api/Application/{applicationId}/users", cancellationToken);
        return await ParseRequiredAsync<List<ApplicationUserResponseDto>>(response, cancellationToken);
    }

    public async Task<InvitationResponseDto> InviteUserAsync(Guid invitedBy, InviteUserRequestDto request, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"api/Application/invite-user?invitedBy={invitedBy}")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
        // Forward X-ClinicApp-Id header if present in the request
        if (request.ApplicationId != Guid.Empty)
        {
            httpRequest.Headers.Add("X-ClinicApp-Id", request.ApplicationId.ToString());
        }
        var response = await client.SendAsync(httpRequest, cancellationToken);
        return await ParseRequiredAsync<InvitationResponseDto>(response, cancellationToken);
    }

    public async Task AssignUserToApplicationAsync(Guid applicationId, Guid userId, Guid roleId, Guid assignedBy, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        var response = await client.PostAsync($"api/Application/{applicationId}/users/{userId}/assign?roleId={roleId}&assignedBy={assignedBy}", null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task AddApplicationUserMappingAsync(Guid applicationId, Guid userId, Guid assignedBy, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        var response = await client.PostAsync($"api/Application/{applicationId}/users/{userId}/mapping?assignedBy={assignedBy}", null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task RemoveUserFromApplicationAsync(Guid applicationId, Guid userId, Guid removedBy, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        var response = await client.DeleteAsync($"api/Application/{applicationId}/users/{userId}?removedBy={removedBy}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task DeleteApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        var response = await client.DeleteAsync($"api/Application/{applicationId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task ToggleApplicationStatusAsync(Guid applicationId, Guid modifiedBy, bool isActive, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Patch, $"api/Application/{applicationId}/status?modifiedBy={modifiedBy}")
        {
            Content = JsonContent.Create(new { isActive }, options: JsonOptions)
        };
        request.Headers.Add("X-ClinicApp-Id", applicationId.ToString());
        var response = await client.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task ToggleUserApplicationMappingAsync(Guid applicationId, Guid userId, Guid modifiedBy, bool isActive, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Patch, $"api/Application/{applicationId}/users/{userId}/status?modifiedBy={modifiedBy}")
        {
            Content = JsonContent.Create(new { isActive }, options: JsonOptions)
        };
        request.Headers.Add("X-ClinicApp-Id", applicationId.ToString());
        var response = await client.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task ChangeUserRoleAsync(Guid applicationId, Guid userId, Guid modifiedBy, Guid newRoleId, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Put, $"api/Application/{applicationId}/users/{userId}/role?modifiedBy={modifiedBy}")
        {
            Content = JsonContent.Create(new { newRoleId }, options: JsonOptions)
        };
        request.Headers.Add("X-ClinicApp-Id", applicationId.ToString());
        var response = await client.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task InviteExistingUserAsync(Guid invitedBy, InviteExistingUserRequestDto request, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"api/Application/invite-existing-user?invitedBy={invitedBy}")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
        if (request.ApplicationId != Guid.Empty)
        {
            httpRequest.Headers.Add("X-ClinicApp-Id", request.ApplicationId.ToString());
        }
        var response = await client.SendAsync(httpRequest, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<List<ApplicationUserResponseDto>> GetDeactivatedApplicationUsersAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        var response = await client.GetAsync($"api/Application/{applicationId}/users/deactivated", cancellationToken);
        return await ParseRequiredAsync<List<ApplicationUserResponseDto>>(response, cancellationToken);
    }

    private async Task<T> ParseRequiredAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Auth API returned {StatusCode}: {Body}", (int)response.StatusCode, body);
            throw new InvalidOperationException($"Auth API error ({(int)response.StatusCode}): {body}");
        }

        try
        {
            var wrapper = JsonSerializer.Deserialize<ApiResponse<T>>(body, JsonOptions);
            if (wrapper is null || (!wrapper.Success && wrapper.Data is null))
            {
                var message = wrapper?.Message ?? "Unexpected empty response from Auth API.";
                throw new InvalidOperationException(message);
            }
            return wrapper.Data!;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize Auth API response: {Body}", body);
            throw new InvalidOperationException("Failed to parse response from Auth API.", ex);
        }
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("Auth API returned {StatusCode}: {Body}", (int)response.StatusCode, body);
            throw new InvalidOperationException($"Auth API error ({(int)response.StatusCode}): {body}");
        }
    }
}
