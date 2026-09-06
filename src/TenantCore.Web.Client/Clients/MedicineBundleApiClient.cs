using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;
using TenantCore.Web.Client.Services;

namespace TenantCore.Web.Client.Clients;

public class MedicineBundleApiClient(HttpClient httpClient, AuthStateService authState) : IMedicineBundleApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private void SetAuth() =>
        httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(authState.AccessToken)
            ? null
            : new AuthenticationHeaderValue("Bearer", authState.AccessToken);

    private static async Task<ApiResponse<T>> Ok<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            return new ApiResponse<T> { Success = false, Message = err };
        }
        var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        return new ApiResponse<T> { Success = true, Data = data };
    }

    private static ApiResponse<T> Fail<T>(string message) =>
        new() { Success = false, Message = message, Errors = [message] };

    public async Task<ApiResponse<IEnumerable<MedicineBundleDto>>> GetAllAsync()
    {
        try { SetAuth(); return await Ok<IEnumerable<MedicineBundleDto>>(await httpClient.GetAsync("api/medicine-bundles")); }
        catch (Exception ex) { return Fail<IEnumerable<MedicineBundleDto>>(ex.Message); }
    }

    public async Task<ApiResponse<MedicineBundleDto>> GetByIdAsync(Guid id)
    {
        try { SetAuth(); return await Ok<MedicineBundleDto>(await httpClient.GetAsync($"api/medicine-bundles/{id}")); }
        catch (Exception ex) { return Fail<MedicineBundleDto>(ex.Message); }
    }

    public async Task<ApiResponse<MedicineBundleDto>> CreateAsync(CreateMedicineBundleDto dto)
    {
        try { SetAuth(); return await Ok<MedicineBundleDto>(await httpClient.PostAsJsonAsync("api/medicine-bundles", dto, JsonOptions)); }
        catch (Exception ex) { return Fail<MedicineBundleDto>(ex.Message); }
    }

    public async Task<ApiResponse<MedicineBundleDto>> UpdateAsync(Guid id, UpdateMedicineBundleDto dto)
    {
        try { SetAuth(); return await Ok<MedicineBundleDto>(await httpClient.PutAsJsonAsync($"api/medicine-bundles/{id}", dto, JsonOptions)); }
        catch (Exception ex) { return Fail<MedicineBundleDto>(ex.Message); }
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
    {
        try
        {
            SetAuth();
            var response = await httpClient.DeleteAsync($"api/medicine-bundles/{id}");
            return response.IsSuccessStatusCode
                ? new ApiResponse<bool> { Success = true, Data = true }
                : new ApiResponse<bool> { Success = false, Message = await response.Content.ReadAsStringAsync() };
        }
        catch (Exception ex) { return Fail<bool>(ex.Message); }
    }
}
