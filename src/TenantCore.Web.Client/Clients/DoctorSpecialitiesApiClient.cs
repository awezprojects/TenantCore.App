using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;
using TenantCore.Web.Client.Services;

namespace TenantCore.Web.Client.Clients;

public class DoctorSpecialitiesApiClient(HttpClient httpClient, AuthStateService authState)
    : IDoctorSpecialitiesApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private void SetAuth() =>
        httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(authState.AccessToken)
            ? null
            : new AuthenticationHeaderValue("Bearer", authState.AccessToken);

    public async Task<ApiResponse<List<DoctorSpecialityDto>>> GetAllAsync()
    {
        try
        {
            SetAuth();
            var response = await httpClient.GetAsync("api/doctor-specialities");
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                return new ApiResponse<List<DoctorSpecialityDto>> { Success = false, Message = err };
            }
            var data = await response.Content.ReadFromJsonAsync<List<DoctorSpecialityDto>>(JsonOptions);
            return new ApiResponse<List<DoctorSpecialityDto>> { Success = true, Data = data ?? [] };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<DoctorSpecialityDto>> { Success = false, Message = ex.Message };
        }
    }
}
