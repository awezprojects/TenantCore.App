using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;
using TenantCore.Web.Client.Services;

namespace TenantCore.Web.Client.Clients;

public class PregnancyTenureApiClient(HttpClient httpClient, AuthStateService authState) : IPregnancyTenureApiClient
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

    public async Task<ApiResponse<IEnumerable<PregnancyTenureSummaryDto>>> GetOverdueAsync()
    {
        try
        {
            SetAuth();
            var response = await httpClient.GetAsync("api/pregnancytenures/overdue");
            return await Ok<IEnumerable<PregnancyTenureSummaryDto>>(response);
        }
        catch (Exception ex) { return Fail<IEnumerable<PregnancyTenureSummaryDto>>(ex.Message); }
    }

    public async Task<ApiResponse<PregnancyTenureDto?>> GetActiveForPatientAsync(Guid patientId)
    {
        try
        {
            SetAuth();
            var response = await httpClient.GetAsync($"api/pregnancytenures/active/{patientId}");
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                return new ApiResponse<PregnancyTenureDto?> { Success = true, Data = null };
            return await Ok<PregnancyTenureDto?>(response);
        }
        catch (Exception ex) { return Fail<PregnancyTenureDto?>(ex.Message); }
    }

    public async Task<ApiResponse<IEnumerable<PregnancyTenureDto>>> GetAllForPatientAsync(Guid patientId)
    {
        try
        {
            SetAuth();
            var response = await httpClient.GetAsync($"api/pregnancytenures/patient/{patientId}");
            return await Ok<IEnumerable<PregnancyTenureDto>>(response);
        }
        catch (Exception ex) { return Fail<IEnumerable<PregnancyTenureDto>>(ex.Message); }
    }

    public async Task<ApiResponse<PregnancyTenureDto>> CloseAsync(Guid tenureId, ClosePregnancyTenureRequest request)
    {
        try
        {
            SetAuth();
            var response = await httpClient.PutAsJsonAsync($"api/pregnancytenures/{tenureId}/close", request, JsonOptions);
            return await Ok<PregnancyTenureDto>(response);
        }
        catch (Exception ex) { return Fail<PregnancyTenureDto>(ex.Message); }
    }
}
