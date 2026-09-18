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

    private static Task<ApiResponse<T>> Read<T>(HttpResponseMessage response)
        => ApiResponseReader.ReadAsync<T>(response);

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

    public async Task<ApiResponse<DoctorProfileDto>> GetByUserIdAsync(Guid userId)
    {
        try
        {
            SetAuth();
            var response = await httpClient.GetAsync($"api/doctor-profile/by-user/{userId}");
            return await Read<DoctorProfileDto>(response);
        }
        catch (Exception ex)
        {
            return new ApiResponse<DoctorProfileDto> { Success = false, Message = ex.Message };
        }
    }

    public async Task<ApiResponse<DoctorProfileDto>> SetMyPrescriptionTemplateAsync(SetPrescriptionTemplateDto dto)
    {
        try
        {
            SetAuth();
            var response = await httpClient.PutAsJsonAsync("api/doctor-profile/template", dto, JsonOptions);
            return await Read<DoctorProfileDto>(response);
        }
        catch (Exception ex)
        {
            return new ApiResponse<DoctorProfileDto> { Success = false, Message = ex.Message };
        }
    }
}
