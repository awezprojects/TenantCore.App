using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;
using TenantCore.Web.Client.Services;

namespace TenantCore.Web.Client.Clients;

public class DoctorProfileApiClient(HttpClient httpClient, AuthStateService authState) : IDoctorProfileApiClient
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

    public async Task<ApiResponse<DoctorProfileDto>> GetMyProfileAsync()
    {
        try
        {
            SetAuth();
            var response = await httpClient.GetAsync("api/doctor-profile");
            return await Read<DoctorProfileDto>(response);
        }
        catch (Exception ex)
        {
            return new ApiResponse<DoctorProfileDto> { Success = false, Message = ex.Message };
        }
    }

    public async Task<ApiResponse<DoctorProfileDto>> UpsertMyProfileAsync(UpsertDoctorProfileDto dto)
    {
        try
        {
            SetAuth();
            var response = await httpClient.PutAsJsonAsync("api/doctor-profile", dto, JsonOptions);
            return await Read<DoctorProfileDto>(response);
        }
        catch (Exception ex)
        {
            return new ApiResponse<DoctorProfileDto> { Success = false, Message = ex.Message };
        }
    }
}
