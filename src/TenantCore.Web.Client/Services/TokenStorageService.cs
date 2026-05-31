using Microsoft.JSInterop;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Web.Client.Services;

/// <summary>
/// Service for persisting authentication tokens in browser local storage.
/// </summary>
public class TokenStorageService
{
    private readonly IJSRuntime _jsRuntime;
    private const string AccessTokenKey       = "tenantcore_access_token";
    private const string UserDataKey          = "tenantcore_user_data";
    private const string TokenExpiryKey       = "tenantcore_token_expiry";
    private const string AvailableAppsKey     = "tenantcore_available_apps";

    public TokenStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Stores authentication tokens, user data, and available applications in local storage.
    /// </summary>
    public async Task StoreTokensAsync(LoginResponseDto response)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", AccessTokenKey, response.AccessToken ?? "");
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenExpiryKey, response.ExpiresAt.ToString("O"));

            if (response.User != null)
            {
                var userJson = System.Text.Json.JsonSerializer.Serialize(response.User);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", UserDataKey, userJson);
            }

            var appsJson = System.Text.Json.JsonSerializer.Serialize(response.AvailableApplications);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", AvailableAppsKey, appsJson);
        }
        catch
        {
            // Ignore errors - likely prerendering
        }
    }

    /// <summary>
    /// Retrieves the stored access token.
    /// </summary>
    public async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", AccessTokenKey);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Retrieves the token expiry date.
    /// </summary>
    public async Task<DateTime?> GetTokenExpiryAsync()
    {
        try
        {
            var expiryStr = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenExpiryKey);
            if (DateTime.TryParse(expiryStr, out var expiry))
            {
                return expiry;
            }
        }
        catch
        {
            // Ignore
        }
        return null;
    }

    /// <summary>
    /// Retrieves the stored user profile.
    /// </summary>
    public async Task<UserProfileDto?> GetUserAsync()
    {
        try
        {
            var userJson = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", UserDataKey);
            if (!string.IsNullOrEmpty(userJson))
            {
                return System.Text.Json.JsonSerializer.Deserialize<UserProfileDto>(userJson);
            }
        }
        catch
        {
            // Ignore
        }
        return null;
    }

    /// <summary>
    /// Retrieves the persisted list of applications the user is linked to.
    /// </summary>
    public async Task<List<ApplicationDto>> GetAvailableApplicationsAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", AvailableAppsKey);
            if (!string.IsNullOrEmpty(json))
                return System.Text.Json.JsonSerializer.Deserialize<List<ApplicationDto>>(json) ?? [];
        }
        catch { }
        return [];
    }

    /// <summary>
    /// Checks if the stored token is still valid.
    /// </summary>
    public async Task<bool> IsTokenValidAsync()
    {
        try
        {
            var token = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token)) return false;

            var expiry = await GetTokenExpiryAsync();
            return expiry.HasValue && expiry.Value > DateTime.UtcNow;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Clears all stored authentication data.
    /// </summary>
    public async Task ClearTokensAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AccessTokenKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", UserDataKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenExpiryKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AvailableAppsKey);
        }
        catch
        {
            // Ignore
        }
    }
}
