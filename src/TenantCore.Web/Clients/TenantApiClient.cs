using System.Net.Http.Json;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Web.Clients;

public class TenantApiClient(HttpClient httpClient) : ITenantApiClient
{
    public async Task<PagedResult<TenantDto>> GetTenantsAsync(int page = 1, int pageSize = 10, string? search = null)
    {
        var url = $"api/tenants?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(search))
            url += $"&search={Uri.EscapeDataString(search)}";
        return await httpClient.GetFromJsonAsync<PagedResult<TenantDto>>(url) ?? new();
    }

    public async Task<TenantDto?> GetTenantByIdAsync(Guid id)
        => await httpClient.GetFromJsonAsync<TenantDto>($"api/tenants/{id}");

    public async Task<TenantDto> CreateTenantAsync(CreateTenantDto dto)
    {
        var response = await httpClient.PostAsJsonAsync("api/tenants", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TenantDto>() ?? throw new InvalidOperationException("Null response");
    }

    public async Task<TenantDto> UpdateTenantAsync(Guid id, UpdateTenantDto dto)
    {
        var response = await httpClient.PutAsJsonAsync($"api/tenants/{id}", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TenantDto>() ?? throw new InvalidOperationException("Null response");
    }

    public async Task DeleteTenantAsync(Guid id)
    {
        var response = await httpClient.DeleteAsync($"api/tenants/{id}");
        response.EnsureSuccessStatusCode();
    }
}
