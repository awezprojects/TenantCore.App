using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TenantCore.Shared.Dtos.Auth;
using TenantCore.Web.Client.Services;

namespace TenantCore.Web.Client.Clients;

/// <summary>
/// HTTP client implementation for Application API in WebAssembly.
/// Calls the wrapper API (TenantCore.Api) which proxies to TenantCore.Auth.
/// </summary>
public class ApplicationApiClient(HttpClient httpClient, AuthStateService authState) : IApplicationApiClient
{
    private const string BaseRoute = "api/Application";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ApiResponse<ApplicationResponseDto>> CreateApplicationAsync(Guid ownerId, ApplicationCreationRequestDto request)
    {
        try
        {
            SetAuthorizationHeader();
            var response = await httpClient.PostAsJsonAsync($"{BaseRoute}?ownerId={ownerId}", request, JsonOptions);
            return await HandleResponse<ApplicationResponseDto>(response);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<ApplicationResponseDto>($"Create application failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<ApplicationResponseDto>> EditApplicationAsync(Guid applicationId, ApplicationCreationRequestDto request)
    {
        try
        {
            SetAuthorizationHeader();
            var response = await httpClient.PutAsJsonAsync($"{BaseRoute}/{applicationId}", request, JsonOptions);
            return await HandleResponse<ApplicationResponseDto>(response);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<ApplicationResponseDto>($"Edit application failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<ApplicationResponseDto>> GetApplicationByIdAsync(Guid applicationId)
    {
        try
        {
            SetAuthorizationHeader();
            var response = await httpClient.GetAsync($"{BaseRoute}/{applicationId}");
            return await HandleResponse<ApplicationResponseDto>(response);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<ApplicationResponseDto>($"Get application failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<ApplicationResponseDto>> GetApplicationByCodeAsync(string code)
    {
        try
        {
            SetAuthorizationHeader();
            var response = await httpClient.GetAsync($"{BaseRoute}/by-code/{Uri.EscapeDataString(code)}");
            return await HandleResponse<ApplicationResponseDto>(response);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<ApplicationResponseDto>($"Get application by code failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<ApplicationResponseDto>>> GetAllApplicationsAsync()
    {
        try
        {
            SetAuthorizationHeader();
            var response = await httpClient.GetAsync($"{BaseRoute}/get-all");
            return await HandleResponse<List<ApplicationResponseDto>>(response);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<List<ApplicationResponseDto>>($"Get all applications failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<ApplicationResponseDto>>> GetApplicationsByTypeAsync(int applicationType)
    {
        try
        {
            SetAuthorizationHeader();
            var response = await httpClient.GetAsync($"{BaseRoute}/by-type/{applicationType}");
            return await HandleResponse<List<ApplicationResponseDto>>(response);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<List<ApplicationResponseDto>>($"Get applications by type failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<ApplicationUserResponseDto>>> GetApplicationUsersAsync(Guid applicationId)
    {
        try
        {
            SetAuthorizationHeader();
            var response = await httpClient.GetAsync($"{BaseRoute}/{applicationId}/users");
            return await HandleResponse<List<ApplicationUserResponseDto>>(response);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<List<ApplicationUserResponseDto>>($"Get application users failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<InvitationResponseDto>> InviteUserAsync(Guid invitedBy, InviteUserRequestDto request)
    {
        try
        {
            SetAuthorizationHeader();
            var response = await httpClient.PostAsJsonAsync($"{BaseRoute}/invite-user?invitedBy={invitedBy}", request, JsonOptions);
            return await HandleResponse<InvitationResponseDto>(response);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<InvitationResponseDto>($"Invite user failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse> AssignUserToApplicationAsync(Guid applicationId, Guid userId, Guid roleId, Guid assignedBy)
    {
        try
        {
            SetAuthorizationHeader();
            var response = await httpClient.PostAsync(
                $"{BaseRoute}/{applicationId}/users/{userId}/assign?roleId={roleId}&assignedBy={assignedBy}",
                null);
            return await HandleBasicResponse(response);
        }
        catch (Exception ex)
        {
            return CreateBasicErrorResponse($"Assign user failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse> AddApplicationUserMappingAsync(Guid applicationId, Guid userId, Guid assignedBy)
    {
        try
        {
            SetAuthorizationHeader();
            var response = await httpClient.PostAsync(
                $"{BaseRoute}/{applicationId}/users/{userId}/mapping?assignedBy={assignedBy}",
                null);
            return await HandleBasicResponse(response);
        }
        catch (Exception ex)
        {
            return CreateBasicErrorResponse($"Add user mapping failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse> RemoveUserFromApplicationAsync(Guid applicationId, Guid userId, Guid removedBy)
    {
        try
        {
            SetAuthorizationHeader();
            var response = await httpClient.DeleteAsync(
                $"{BaseRoute}/{applicationId}/users/{userId}?removedBy={removedBy}");
            return await HandleBasicResponse(response);
        }
        catch (Exception ex)
        {
            return CreateBasicErrorResponse($"Remove user failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse> DeleteApplicationAsync(Guid applicationId)
    {
        try
        {
            SetAuthorizationHeader();
            var response = await httpClient.DeleteAsync($"{BaseRoute}/{applicationId}");
            return await HandleBasicResponse(response);
        }
        catch (Exception ex)
        {
            return CreateBasicErrorResponse($"Delete application failed: {ex.Message}");
        }
    }

    private void SetAuthorizationHeader()
    {
        if (!string.IsNullOrEmpty(authState.AccessToken))
        {
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", authState.AccessToken);
        }
    }

    private static async Task<ApiResponse<T>> HandleResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrEmpty(content))
        {
            return new ApiResponse<T>
            {
                Success = response.IsSuccessStatusCode,
                Message = response.IsSuccessStatusCode ? "Success" : $"Request failed with status {response.StatusCode}"
            };
        }

        try
        {
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
            return result ?? new ApiResponse<T>
            {
                Success = false,
                Message = "Failed to deserialize response"
            };
        }
        catch
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = content
            };
        }
    }

    private static async Task<ApiResponse> HandleBasicResponse(HttpResponseMessage response)
    {
        try
        {
            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
            return result ?? new ApiResponse
            {
                Success = response.IsSuccessStatusCode,
                Message = response.IsSuccessStatusCode ? "Success" : "Request failed"
            };
        }
        catch
        {
            return new ApiResponse
            {
                Success = response.IsSuccessStatusCode,
                Message = response.IsSuccessStatusCode ? "Success" : "Request failed"
            };
        }
    }

    private static ApiResponse<T> CreateErrorResponse<T>(string message) => new()
    {
        Success = false,
        Message = message,
        Errors = [message]
    };

    private static ApiResponse CreateBasicErrorResponse(string message) => new()
    {
        Success = false,
        Message = message,
        Errors = [message]
    };
}
