using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TenantCore.Shared.Dtos.Auth;
using TenantCore.Shared.Dtos.Subscriptions;
using TenantCore.Web.Client.Services;

namespace TenantCore.Web.Client.Clients;

public class SubscriptionApiClient(HttpClient httpClient, AuthStateService authState) : ISubscriptionApiClient
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

    private static async Task<ApiResponse> ReadVoid(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            return new ApiResponse { Success = false, Message = err };
        }
        return new ApiResponse { Success = true };
    }

    public async Task<ApiResponse<IEnumerable<SubscriptionPlanDto>>> GetPlansAsync()
    {
        try { SetAuth(); return await Read<IEnumerable<SubscriptionPlanDto>>(await httpClient.GetAsync("api/subscriptions/plans")); }
        catch (Exception ex) { return new ApiResponse<IEnumerable<SubscriptionPlanDto>> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<SubscriptionStatusDto>> GetStatusAsync(Guid? applicationId = null)
    {
        try
        {
            SetAuth();
            if (applicationId.HasValue)
            {
                var req = new HttpRequestMessage(HttpMethod.Get, "api/subscriptions/status");
                req.Headers.TryAddWithoutValidation("X-Application-Id", applicationId.Value.ToString());
                return await Read<SubscriptionStatusDto>(await httpClient.SendAsync(req));
            }
            return await Read<SubscriptionStatusDto>(await httpClient.GetAsync("api/subscriptions/status"));
        }
        catch (Exception ex) { return new ApiResponse<SubscriptionStatusDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<IEnumerable<SubscriptionHistoryItemDto>>> GetHistoryAsync()
    {
        try { SetAuth(); return await Read<IEnumerable<SubscriptionHistoryItemDto>>(await httpClient.GetAsync("api/subscriptions/history")); }
        catch (Exception ex) { return new ApiResponse<IEnumerable<SubscriptionHistoryItemDto>> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<ClinicSubscriptionDto>> SubscribeAsync(SubscribeRequest request)
    {
        try { SetAuth(); return await Read<ClinicSubscriptionDto>(await httpClient.PostAsJsonAsync("api/subscriptions/subscribe", request, JsonOptions)); }
        catch (Exception ex) { return new ApiResponse<ClinicSubscriptionDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse> CancelAsync(Guid subscriptionId)
    {
        try { SetAuth(); return await ReadVoid(await httpClient.PostAsync($"api/subscriptions/{subscriptionId}/cancel", null)); }
        catch (Exception ex) { return new ApiResponse { Success = false, Message = ex.Message }; }
    }
}
