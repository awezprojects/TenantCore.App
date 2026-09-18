using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;
using TenantCore.Web.Client.Services;

namespace TenantCore.Web.Client.Clients;

public class DoctorFeeConfigApiClient(HttpClient httpClient, AuthStateService authState) : IDoctorFeeConfigApiClient
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

    public async Task<ApiResponse<IEnumerable<DoctorFeeConfigSummaryDto>>> GetAllAsync()
    {
        try { SetAuth(); return await Read<IEnumerable<DoctorFeeConfigSummaryDto>>(await httpClient.GetAsync("api/doctor-fee-configs")); }
        catch (Exception ex) { return new ApiResponse<IEnumerable<DoctorFeeConfigSummaryDto>> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<DoctorFeeConfigDto>> GetByIdAsync(Guid id)
    {
        try { SetAuth(); return await Read<DoctorFeeConfigDto>(await httpClient.GetAsync($"api/doctor-fee-configs/{id}")); }
        catch (Exception ex) { return new ApiResponse<DoctorFeeConfigDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<DoctorFeeConfigDto>> GetByDoctorIdAsync(Guid doctorProfileId)
    {
        try { SetAuth(); return await Read<DoctorFeeConfigDto>(await httpClient.GetAsync($"api/doctor-fee-configs/by-doctor/{doctorProfileId}")); }
        catch (Exception ex) { return new ApiResponse<DoctorFeeConfigDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<Guid>> CreateAsync(CreateDoctorFeeConfigRequest request)
    {
        try { SetAuth(); return await Read<Guid>(await httpClient.PostAsJsonAsync("api/doctor-fee-configs", request, JsonOptions)); }
        catch (Exception ex) { return new ApiResponse<Guid> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<DoctorFeeConfigDto>> UpdateAsync(Guid id, UpdateDoctorFeeConfigRequest request)
    {
        try { SetAuth(); return await Read<DoctorFeeConfigDto>(await httpClient.PutAsJsonAsync($"api/doctor-fee-configs/{id}", request, JsonOptions)); }
        catch (Exception ex) { return new ApiResponse<DoctorFeeConfigDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse> DeleteAsync(Guid id)
    {
        try { SetAuth(); return await ReadEmpty(await httpClient.DeleteAsync($"api/doctor-fee-configs/{id}")); }
        catch (Exception ex) { return new ApiResponse { Success = false, Message = ex.Message }; }
    }
}
