using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;
using TenantCore.Web.Client.Services;

namespace TenantCore.Web.Client.Clients;

public class VitalPresetApiClient(HttpClient httpClient, AuthStateService authState) : IVitalPresetApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private void SetAuth() =>
        httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(authState.AccessToken)
            ? null
            : new AuthenticationHeaderValue("Bearer", authState.AccessToken);

    private static Task<ApiResponse<T>> Read<T>(HttpResponseMessage response)
        => ApiResponseReader.ReadAsync<T>(response);

    private static Task<ApiResponse> ReadEmpty(HttpResponseMessage response)
        => ApiResponseReader.ReadAsync(response);

    public async Task<ApiResponse<IEnumerable<VitalPresetLookupItemDto>>> GetAllAsync()
    {
        try { SetAuth(); return await Read<IEnumerable<VitalPresetLookupItemDto>>(await httpClient.GetAsync("api/vital-presets")); }
        catch (Exception ex) { return new ApiResponse<IEnumerable<VitalPresetLookupItemDto>> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<VitalPresetLookupItemDto>> AddAsync(AddVitalPresetLookupItemDto dto)
    {
        try { SetAuth(); return await Read<VitalPresetLookupItemDto>(await httpClient.PostAsJsonAsync("api/vital-presets", dto, JsonOptions)); }
        catch (Exception ex) { return new ApiResponse<VitalPresetLookupItemDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse> DeleteAsync(Guid id)
    {
        try { SetAuth(); return await ReadEmpty(await httpClient.DeleteAsync($"api/vital-presets/{id}")); }
        catch (Exception ex) { return new ApiResponse { Success = false, Message = ex.Message }; }
    }
}
