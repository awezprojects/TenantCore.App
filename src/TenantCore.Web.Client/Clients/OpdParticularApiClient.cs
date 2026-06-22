using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;
using TenantCore.Web.Client.Services;

namespace TenantCore.Web.Client.Clients;

public class OpdParticularApiClient(HttpClient httpClient, AuthStateService authState) : IOpdParticularApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private void SetAuth() =>
        httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(authState.AccessToken)
            ? null
            : new AuthenticationHeaderValue("Bearer", authState.AccessToken);

    private static async Task<ApiResponse<T>> Read<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            return new ApiResponse<T> { Success = false, Message = err };
        }
        var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        return new ApiResponse<T> { Success = true, Data = data };
    }

    private static async Task<ApiResponse> ReadEmpty(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            return new ApiResponse { Success = false, Message = err };
        }
        return new ApiResponse { Success = true };
    }

    public async Task<ApiResponse<IEnumerable<OpdParticularDto>>> GetByOpdAsync(Guid opdRegistrationId)
    {
        try { SetAuth(); return await Read<IEnumerable<OpdParticularDto>>(await httpClient.GetAsync($"api/opd-particulars/by-opd/{opdRegistrationId}")); }
        catch (Exception ex) { return new ApiResponse<IEnumerable<OpdParticularDto>> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<OpdParticularDto>> AddAsync(AddOpdParticularRequest request)
    {
        try { SetAuth(); return await Read<OpdParticularDto>(await httpClient.PostAsJsonAsync("api/opd-particulars", request, JsonOptions)); }
        catch (Exception ex) { return new ApiResponse<OpdParticularDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<OpdParticularDto>> UpdateAsync(Guid id, UpdateOpdParticularRequest request)
    {
        try { SetAuth(); return await Read<OpdParticularDto>(await httpClient.PutAsJsonAsync($"api/opd-particulars/{id}", request, JsonOptions)); }
        catch (Exception ex) { return new ApiResponse<OpdParticularDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse> RemoveAsync(Guid id)
    {
        try { SetAuth(); return await ReadEmpty(await httpClient.DeleteAsync($"api/opd-particulars/{id}")); }
        catch (Exception ex) { return new ApiResponse { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse> CollectAsync(Guid particularId, CollectOpdParticularRequest request)
    {
        try { SetAuth(); return await ReadEmpty(await httpClient.PostAsJsonAsync($"api/opd-particulars/{particularId}/collect", request, JsonOptions)); }
        catch (Exception ex) { return new ApiResponse { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse> CollectAllAsync(Guid opdRegistrationId, CollectAllOpdParticularsRequest request)
    {
        try { SetAuth(); return await ReadEmpty(await httpClient.PostAsJsonAsync($"api/opd-particulars/by-opd/{opdRegistrationId}/collect-all", request, JsonOptions)); }
        catch (Exception ex) { return new ApiResponse { Success = false, Message = ex.Message }; }
    }
}
