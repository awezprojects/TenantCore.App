using System.Net.Http.Headers;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;
using TenantCore.Web.Client.Services;

namespace TenantCore.Web.Client.Clients;

public class DoctorSpecialitiesApiClient(HttpClient httpClient, AuthStateService authState)
    : IDoctorSpecialitiesApiClient
{
    private void SetAuth() =>
        httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(authState.AccessToken)
            ? null
            : new AuthenticationHeaderValue("Bearer", authState.AccessToken);

    private static Task<ApiResponse<T>> ReadAsync<T>(HttpResponseMessage response)
        => ApiResponseReader.ReadAsync<T>(response);

    public async Task<ApiResponse<List<DoctorSpecialityDto>>> GetAllAsync()
    {
        try
        {
            SetAuth();
            var response = await httpClient.GetAsync("api/doctor-specialities");
            return await ReadAsync<List<DoctorSpecialityDto>>(response);
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<DoctorSpecialityDto>> { Success = false, Message = ex.Message };
        }
    }
}
