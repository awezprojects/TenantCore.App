using System.Net.Http.Headers;
using System.Net.Http.Json;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;
using TenantCore.Web.Client.Services;

namespace TenantCore.Web.Client.Clients;

/// <summary>
/// HTTP client for Tenant API operations in WebAssembly.
/// </summary>
public class TenantApiClient(HttpClient httpClient, AuthStateService authState) : ITenantApiClient
{
    public async Task<PagedResult<TenantDto>> GetTenantsAsync(int page = 1, int pageSize = 10, string? search = null)
    {
        SetAuthorizationHeader();
        var url = $"api/tenants?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(search))
            url += $"&search={Uri.EscapeDataString(search)}";
        return await httpClient.GetFromJsonAsync<PagedResult<TenantDto>>(url) ?? new();
    }

    public async Task<TenantDto?> GetTenantByIdAsync(Guid id)
    {
        SetAuthorizationHeader();
        return await httpClient.GetFromJsonAsync<TenantDto>($"api/tenants/{id}");
    }

    public async Task<TenantDto> CreateTenantAsync(CreateTenantDto dto)
    {
        SetAuthorizationHeader();
        var response = await httpClient.PostAsJsonAsync("api/tenants", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TenantDto>() ?? throw new InvalidOperationException("Null response");
    }

    public async Task<TenantDto> UpdateTenantAsync(Guid id, UpdateTenantDto dto)
    {
        SetAuthorizationHeader();
        var response = await httpClient.PutAsJsonAsync($"api/tenants/{id}", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TenantDto>() ?? throw new InvalidOperationException("Null response");
    }

    public async Task DeleteTenantAsync(Guid id)
    {
        SetAuthorizationHeader();
        var response = await httpClient.DeleteAsync($"api/tenants/{id}");
        response.EnsureSuccessStatusCode();
    }

    private void SetAuthorizationHeader()
    {
        if (!string.IsNullOrEmpty(authState.AccessToken))
        {
            httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", authState.AccessToken);
        }
    }
}
