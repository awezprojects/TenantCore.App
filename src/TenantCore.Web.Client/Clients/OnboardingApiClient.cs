using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TenantCore.Shared.Dtos.Auth;
using TenantCore.Shared.Dtos.Onboarding;
using TenantCore.Web.Client.Services;

namespace TenantCore.Web.Client.Clients;

public class OnboardingApiClient(HttpClient httpClient, AuthStateService authState) : IOnboardingApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private void SetAuth() =>
        httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(authState.AccessToken)
            ? null
            : new AuthenticationHeaderValue("Bearer", authState.AccessToken);

    private static Task<ApiResponse<T>> Read<T>(HttpResponseMessage response)
        => ApiResponseReader.ReadAsync<T>(response);

    private static Task<ApiResponse> ReadVoid(HttpResponseMessage response)
        => ApiResponseReader.ReadAsync(response);

    public async Task<ApiResponse<Guid>> SubmitAsync(SubmitClinicOnboardingRequest request)
    {
        try { SetAuth(); return await Read<Guid>(await httpClient.PostAsJsonAsync("api/onboarding/requests", request, JsonOptions)); }
        catch (Exception ex) { return new ApiResponse<Guid> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<IEnumerable<ClinicOnboardingRequestDto>>> GetMineAsync()
    {
        try { SetAuth(); return await Read<IEnumerable<ClinicOnboardingRequestDto>>(await httpClient.GetAsync("api/onboarding/requests/mine")); }
        catch (Exception ex) { return new ApiResponse<IEnumerable<ClinicOnboardingRequestDto>> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<ClinicOnboardingRequestDto>> GetByIdAsync(Guid id)
    {
        try { SetAuth(); return await Read<ClinicOnboardingRequestDto>(await httpClient.GetAsync($"api/onboarding/requests/{id}")); }
        catch (Exception ex) { return new ApiResponse<ClinicOnboardingRequestDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse> CancelAsync(Guid id)
    {
        try { SetAuth(); return await ReadVoid(await httpClient.PostAsync($"api/onboarding/requests/{id}/cancel", null)); }
        catch (Exception ex) { return new ApiResponse { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse> CheckPaymentAsync(Guid id)
    {
        try { SetAuth(); return await ReadVoid(await httpClient.PostAsync($"api/onboarding/requests/{id}/check-payment", null)); }
        catch (Exception ex) { return new ApiResponse { Success = false, Message = ex.Message }; }
    }
}
