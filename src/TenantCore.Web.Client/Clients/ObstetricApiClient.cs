using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;
using TenantCore.Web.Client.Services;

namespace TenantCore.Web.Client.Clients;

public class ObstetricApiClient(HttpClient httpClient, AuthStateService authState) : IObstetricApiClient
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

    public async Task<ApiResponse<ObstetricDatesDto>> GetObstetricDatesAsync(Guid prescriptionId)
    {
        try
        {
            SetAuth();
            var response = await httpClient.GetAsync($"api/obstetric/prescriptions/{prescriptionId}/dates");
            return await Ok<ObstetricDatesDto>(response);
        }
        catch (Exception ex) { return Fail<ObstetricDatesDto>(ex.Message); }
    }

    public async Task<ApiResponse<UsgChartDto>> GetUsgChartAsync(Guid patientId)
    {
        try
        {
            SetAuth();
            var response = await httpClient.GetAsync($"api/obstetric/patients/{patientId}/usg-chart");
            return await Ok<UsgChartDto>(response);
        }
        catch (Exception ex) { return Fail<UsgChartDto>(ex.Message); }
    }

    public async Task<ApiResponse<ObstetricDatesDto>> SetLmpAsync(Guid prescriptionId, SetLmpRequest request)
    {
        try
        {
            SetAuth();
            var response = await httpClient.PutAsJsonAsync($"api/obstetric/prescriptions/{prescriptionId}/lmp", request, JsonOptions);
            return await Ok<ObstetricDatesDto>(response);
        }
        catch (Exception ex) { return Fail<ObstetricDatesDto>(ex.Message); }
    }

    public async Task<ApiResponse<ObstetricDatesDto>> SetEddByUsgAsync(Guid prescriptionId, SetEddByUsgRequest request)
    {
        try
        {
            SetAuth();
            var response = await httpClient.PutAsJsonAsync($"api/obstetric/prescriptions/{prescriptionId}/edd-by-usg", request, JsonOptions);
            return await Ok<ObstetricDatesDto>(response);
        }
        catch (Exception ex) { return Fail<ObstetricDatesDto>(ex.Message); }
    }
}
