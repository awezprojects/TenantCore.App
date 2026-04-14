using Microsoft.AspNetCore.Components.WebAssembly.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Web.Client.Clients;

/// <summary>
/// HTTP client implementation for Auth API in WebAssembly.
/// </summary>
public class AuthApiClient(HttpClient httpClient) : IAuthApiClient
{
    private const string BaseRoute = "api/auth";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static void IncludeCookies(HttpRequestMessage request)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
    }

    public async Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request)
    {
        try
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{BaseRoute}/login")
            {
                Content = JsonContent.Create(request, options: JsonOptions)
            };
            IncludeCookies(requestMessage);

            var response = await httpClient.SendAsync(requestMessage);
            return await HandleResponse<LoginResponseDto>(response);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<LoginResponseDto>($"Login failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<LoginResponseDto>> ValidateTwoFactorAsync(string tempToken, ValidateTwoFactorRequestDto request)
    {
        try
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{BaseRoute}/2fa/validate-login")
            {
                Content = JsonContent.Create(request, options: JsonOptions)
            };
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tempToken);
            IncludeCookies(requestMessage);

            var response = await httpClient.SendAsync(requestMessage);
            return await HandleResponse<LoginResponseDto>(response);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<LoginResponseDto>($"2FA validation failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<LoginResponseDto>> RefreshTokenAsync()
    {
        try
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{BaseRoute}/refresh");
            IncludeCookies(requestMessage);

            var response = await httpClient.SendAsync(requestMessage);
            return await HandleResponse<LoginResponseDto>(response);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<LoginResponseDto>($"Token refresh failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse> LogoutAsync()
    {
        try
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{BaseRoute}/logout");
            IncludeCookies(requestMessage);

            var response = await httpClient.SendAsync(requestMessage);
            return await HandleBasicResponse(response);
        }
        catch (Exception ex)
        {
            return CreateBasicErrorResponse($"Logout failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse> LogoutAllSessionsAsync()
    {
        try
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{BaseRoute}/logout-all");
            IncludeCookies(requestMessage);

            var response = await httpClient.SendAsync(requestMessage);
            return await HandleBasicResponse(response);
        }
        catch (Exception ex)
        {
            return CreateBasicErrorResponse($"Logout all sessions failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<UserProfileDto>> RegisterAsync(RegisterRequestDto request)
    {
        try
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{BaseRoute}/register")
            {
                Content = JsonContent.Create(request, options: JsonOptions)
            };
            IncludeCookies(requestMessage);

            var response = await httpClient.SendAsync(requestMessage);
            return await HandleResponse<UserProfileDto>(response);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<UserProfileDto>($"Registration failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse> VerifyEmailAsync(Guid userId, string verificationCode)
    {
        try
        {
            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Post,
                $"{BaseRoute}/verify-email?userId={userId}&verificationCode={Uri.EscapeDataString(verificationCode)}");
            IncludeCookies(requestMessage);

            var response = await httpClient.SendAsync(requestMessage);
            return await HandleBasicResponse(response);
        }
        catch (Exception ex)
        {
            return CreateBasicErrorResponse($"Email verification failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse> ResendEmailVerificationAsync(ResendEmailVerificationRequestDto request)
    {
        try
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{BaseRoute}/resend-email-verification")
            {
                Content = JsonContent.Create(request, options: JsonOptions)
            };
            IncludeCookies(requestMessage);

            var response = await httpClient.SendAsync(requestMessage);
            return await HandleBasicResponse(response);
        }
        catch (Exception ex)
        {
            return CreateBasicErrorResponse($"Resend verification failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse> ResetPasswordAsync(Guid userId, ResetPasswordRequestDto request)
    {
        try
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{BaseRoute}/reset-password?userId={userId}")
            {
                Content = JsonContent.Create(request, options: JsonOptions)
            };
            IncludeCookies(requestMessage);

            var response = await httpClient.SendAsync(requestMessage);
            return await HandleBasicResponse(response);
        }
        catch (Exception ex)
        {
            return CreateBasicErrorResponse($"Password reset failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<UserProfileDto>> AcceptInvitationAsync(AcceptInvitationRequestDto request)
    {
        try
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{BaseRoute}/accept-invitation")
            {
                Content = JsonContent.Create(request, options: JsonOptions)
            };
            IncludeCookies(requestMessage);

            var response = await httpClient.SendAsync(requestMessage);
            return await HandleResponse<UserProfileDto>(response);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<UserProfileDto>($"Accept invitation failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<UserProfileDto>> GetUserByIdAsync(Guid userId)
    {
        try
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{BaseRoute}/user/{userId}");
            IncludeCookies(requestMessage);

            var response = await httpClient.SendAsync(requestMessage);
            return await HandleResponse<UserProfileDto>(response);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<UserProfileDto>($"Get user failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse> DisableTwoFactorAsync(Guid userId, DisableTwoFactorRequestDto request)
    {
        try
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{BaseRoute}/2fa/disable/{userId}")
            {
                Content = JsonContent.Create(request, options: JsonOptions)
            };
            IncludeCookies(requestMessage);

            var response = await httpClient.SendAsync(requestMessage);
            return await HandleBasicResponse(response);
        }
        catch (Exception ex)
        {
            return CreateBasicErrorResponse($"Disable 2FA failed: {ex.Message}");
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
