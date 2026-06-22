using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;
using TenantCore.Web.Client.Services;

namespace TenantCore.Web.Client.Clients;

public class ParticularApiClient(HttpClient httpClient, AuthStateService authState) : IParticularApiClient
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

    public async Task<ApiResponse<IEnumerable<ParticularSummaryDto>>> GetAllAsync()
    {
        try { SetAuth(); return await Read<IEnumerable<ParticularSummaryDto>>(await httpClient.GetAsync("api/particulars")); }
        catch (Exception ex) { return new ApiResponse<IEnumerable<ParticularSummaryDto>> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<ParticularDto>> GetByIdAsync(Guid id)
    {
        try { SetAuth(); return await Read<ParticularDto>(await httpClient.GetAsync($"api/particulars/{id}")); }
        catch (Exception ex) { return new ApiResponse<ParticularDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<Guid>> CreateAsync(CreateParticularRequest request)
    {
        try { SetAuth(); return await Read<Guid>(await httpClient.PostAsJsonAsync("api/particulars", request, JsonOptions)); }
        catch (Exception ex) { return new ApiResponse<Guid> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<ParticularDto>> UpdateAsync(Guid id, UpdateParticularRequest request)
    {
        try { SetAuth(); return await Read<ParticularDto>(await httpClient.PutAsJsonAsync($"api/particulars/{id}", request, JsonOptions)); }
        catch (Exception ex) { return new ApiResponse<ParticularDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse> DeleteAsync(Guid id)
    {
        try { SetAuth(); return await ReadEmpty(await httpClient.DeleteAsync($"api/particulars/{id}")); }
        catch (Exception ex) { return new ApiResponse { Success = false, Message = ex.Message }; }
    }
}
