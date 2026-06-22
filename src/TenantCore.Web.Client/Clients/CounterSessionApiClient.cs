using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;
using TenantCore.Web.Client.Services;

namespace TenantCore.Web.Client.Clients;

public class CounterSessionApiClient(HttpClient httpClient, AuthStateService authState) : ICounterSessionApiClient
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

    public async Task<ApiResponse<IEnumerable<CounterSessionSummaryDto>>> GetAllAsync(DateTime? from = null, DateTime? to = null)
    {
        try
        {
            SetAuth();
            var url = "api/counter-sessions";
            var qs = new List<string>();
            if (from.HasValue) qs.Add($"from={from.Value:yyyy-MM-dd}");
            if (to.HasValue) qs.Add($"to={to.Value:yyyy-MM-dd}");
            if (qs.Count > 0) url += "?" + string.Join("&", qs);
            return await Read<IEnumerable<CounterSessionSummaryDto>>(await httpClient.GetAsync(url));
        }
        catch (Exception ex) { return new ApiResponse<IEnumerable<CounterSessionSummaryDto>> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<IEnumerable<CounterSessionSummaryDto>>> GetAllActiveAsync()
    {
        try { SetAuth(); return await Read<IEnumerable<CounterSessionSummaryDto>>(await httpClient.GetAsync("api/counter-sessions/all-active")); }
        catch (Exception ex) { return new ApiResponse<IEnumerable<CounterSessionSummaryDto>> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<CounterSessionDto>> GetByIdAsync(Guid id)
    {
        try { SetAuth(); return await Read<CounterSessionDto>(await httpClient.GetAsync($"api/counter-sessions/{id}")); }
        catch (Exception ex) { return new ApiResponse<CounterSessionDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<CounterSessionDto>> GetActiveAsync()
    {
        try { SetAuth(); return await Read<CounterSessionDto>(await httpClient.GetAsync("api/counter-sessions/active")); }
        catch (Exception ex) { return new ApiResponse<CounterSessionDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<Guid>> OpenAsync(OpenCounterSessionRequest request)
    {
        try { SetAuth(); return await Read<Guid>(await httpClient.PostAsJsonAsync("api/counter-sessions/open", request, JsonOptions)); }
        catch (Exception ex) { return new ApiResponse<Guid> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<CounterSessionDto>> CloseAsync(Guid id)
    {
        try { SetAuth(); return await Read<CounterSessionDto>(await httpClient.PostAsJsonAsync($"api/counter-sessions/{id}/close", new { }, JsonOptions)); }
        catch (Exception ex) { return new ApiResponse<CounterSessionDto> { Success = false, Message = ex.Message }; }
    }
}
