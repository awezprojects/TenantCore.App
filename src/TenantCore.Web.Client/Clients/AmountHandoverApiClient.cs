using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;
using TenantCore.Web.Client.Services;

namespace TenantCore.Web.Client.Clients;

public class AmountHandoverApiClient(HttpClient httpClient, AuthStateService authState) : IAmountHandoverApiClient
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

    public async Task<ApiResponse<IEnumerable<AmountHandoverSummaryDto>>> GetAllAsync(DateTime? from = null, DateTime? to = null)
    {
        try
        {
            SetAuth();
            var url = "api/amount-handovers";
            var qs = new List<string>();
            if (from.HasValue) qs.Add($"from={from.Value:yyyy-MM-dd}");
            if (to.HasValue) qs.Add($"to={to.Value:yyyy-MM-dd}");
            qs.Add($"utcOffset={(int)TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalMinutes}");
            url += "?" + string.Join("&", qs);
            return await Read<IEnumerable<AmountHandoverSummaryDto>>(await httpClient.GetAsync(url));
        }
        catch (Exception ex) { return new ApiResponse<IEnumerable<AmountHandoverSummaryDto>> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<IEnumerable<AmountHandoverSummaryDto>>> GetPendingAsync()
    {
        try { SetAuth(); return await Read<IEnumerable<AmountHandoverSummaryDto>>(await httpClient.GetAsync("api/amount-handovers/pending")); }
        catch (Exception ex) { return new ApiResponse<IEnumerable<AmountHandoverSummaryDto>> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<IEnumerable<AmountHandoverSummaryDto>>> GetBySessionAsync(Guid counterSessionId)
    {
        try { SetAuth(); return await Read<IEnumerable<AmountHandoverSummaryDto>>(await httpClient.GetAsync($"api/amount-handovers/by-session/{counterSessionId}")); }
        catch (Exception ex) { return new ApiResponse<IEnumerable<AmountHandoverSummaryDto>> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<AmountHandoverDto>> GetByIdAsync(Guid id)
    {
        try { SetAuth(); return await Read<AmountHandoverDto>(await httpClient.GetAsync($"api/amount-handovers/{id}")); }
        catch (Exception ex) { return new ApiResponse<AmountHandoverDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<Guid>> CreateAsync(CreateAmountHandoverRequest request)
    {
        try { SetAuth(); return await Read<Guid>(await httpClient.PostAsJsonAsync("api/amount-handovers", request, JsonOptions)); }
        catch (Exception ex) { return new ApiResponse<Guid> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<Guid>> CollectAsync(CollectAmountHandoverRequest request)
    {
        try { SetAuth(); return await Read<Guid>(await httpClient.PostAsJsonAsync("api/amount-handovers/collect", request, JsonOptions)); }
        catch (Exception ex) { return new ApiResponse<Guid> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<AmountHandoverDto>> AcceptAsync(Guid id)
    {
        try { SetAuth(); return await Read<AmountHandoverDto>(await httpClient.PostAsJsonAsync($"api/amount-handovers/{id}/accept", new { }, JsonOptions)); }
        catch (Exception ex) { return new ApiResponse<AmountHandoverDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<AmountHandoverDto>> DisputeAsync(Guid id, ResolveAmountHandoverRequest request)
    {
        try { SetAuth(); return await Read<AmountHandoverDto>(await httpClient.PostAsJsonAsync($"api/amount-handovers/{id}/dispute", request, JsonOptions)); }
        catch (Exception ex) { return new ApiResponse<AmountHandoverDto> { Success = false, Message = ex.Message }; }
    }
}
