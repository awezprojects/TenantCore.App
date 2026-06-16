using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;
using TenantCore.Web.Client.Services;

namespace TenantCore.Web.Client.Clients;

public class UsgTemplateApiClient(HttpClient httpClient, AuthStateService authState) : IUsgTemplateApiClient
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

    public async Task<ApiResponse<ClinicUsgTemplateDto>> GetClinicTemplateAsync()
    {
        try
        {
            SetAuth();
            var response = await httpClient.GetAsync("api/usg-templates");
            return await Ok<ClinicUsgTemplateDto>(response);
        }
        catch (Exception ex) { return Fail<ClinicUsgTemplateDto>(ex.Message); }
    }

    public async Task<ApiResponse<ClinicUsgTemplateDto>> GetDefaultTemplateAsync()
    {
        try
        {
            SetAuth();
            var response = await httpClient.GetAsync("api/usg-templates/default");
            return await Ok<ClinicUsgTemplateDto>(response);
        }
        catch (Exception ex) { return Fail<ClinicUsgTemplateDto>(ex.Message); }
    }

    public async Task<ApiResponse<ClinicUsgTemplateDto>> UpsertAsync(UpsertClinicUsgTemplateRequest request)
    {
        try
        {
            SetAuth();
            var response = await httpClient.PutAsJsonAsync("api/usg-templates", request, JsonOptions);
            return await Ok<ClinicUsgTemplateDto>(response);
        }
        catch (Exception ex) { return Fail<ClinicUsgTemplateDto>(ex.Message); }
    }

    public async Task<ApiResponse<ClinicUsgTemplateDto>> ResetAsync()
    {
        try
        {
            SetAuth();
            var response = await httpClient.DeleteAsync("api/usg-templates");
            return await Ok<ClinicUsgTemplateDto>(response);
        }
        catch (Exception ex) { return Fail<ClinicUsgTemplateDto>(ex.Message); }
    }
}
