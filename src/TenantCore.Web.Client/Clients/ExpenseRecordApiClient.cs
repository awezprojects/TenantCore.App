using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;
using TenantCore.Web.Client.Services;

namespace TenantCore.Web.Client.Clients;

public class ExpenseRecordApiClient(HttpClient httpClient, AuthStateService authState) : IExpenseRecordApiClient
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

    private static async Task<ApiResponse> ReadEmpty(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            return new ApiResponse { Success = false, Message = err };
        }
        return new ApiResponse { Success = true };
    }

    public async Task<ApiResponse<IEnumerable<ExpenseRecordSummaryDto>>> GetAllAsync(DateTime? from = null, DateTime? to = null)
    {
        try
        {
            SetAuth();
            var url = "api/expense-records";
            var qs = new List<string>();
            if (from.HasValue) qs.Add($"from={from.Value:yyyy-MM-dd}");
            if (to.HasValue) qs.Add($"to={to.Value:yyyy-MM-dd}");
            // Always send the browser's UTC offset so the server can convert local dates to the
            // correct UTC range when filtering against UTC-stored RecordedAt values.
            qs.Add($"utcOffset={(int)TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalMinutes}");
            url += "?" + string.Join("&", qs);
            return await Read<IEnumerable<ExpenseRecordSummaryDto>>(await httpClient.GetAsync(url));
        }
        catch (Exception ex) { return new ApiResponse<IEnumerable<ExpenseRecordSummaryDto>> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<ExpenseRecordDto>> GetByIdAsync(Guid id)
    {
        try { SetAuth(); return await Read<ExpenseRecordDto>(await httpClient.GetAsync($"api/expense-records/{id}")); }
        catch (Exception ex) { return new ApiResponse<ExpenseRecordDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<IEnumerable<ExpenseRecordSummaryDto>>> GetBySessionAsync(Guid sessionId)
    {
        try { SetAuth(); return await Read<IEnumerable<ExpenseRecordSummaryDto>>(await httpClient.GetAsync($"api/expense-records/by-session/{sessionId}")); }
        catch (Exception ex) { return new ApiResponse<IEnumerable<ExpenseRecordSummaryDto>> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<Guid>> CreateAsync(CreateExpenseRecordRequest request)
    {
        try { SetAuth(); return await Read<Guid>(await httpClient.PostAsJsonAsync("api/expense-records", request, JsonOptions)); }
        catch (Exception ex) { return new ApiResponse<Guid> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<ExpenseRecordDto>> PayAsync(Guid id, PayExpenseRecordRequest request)
    {
        try { SetAuth(); return await Read<ExpenseRecordDto>(await httpClient.PostAsJsonAsync($"api/expense-records/{id}/pay", request, JsonOptions)); }
        catch (Exception ex) { return new ApiResponse<ExpenseRecordDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<ExpenseRecordDto>> UpdateAsync(Guid id, UpdateExpenseRecordRequest request)
    {
        try { SetAuth(); return await Read<ExpenseRecordDto>(await httpClient.PutAsJsonAsync($"api/expense-records/{id}", request, JsonOptions)); }
        catch (Exception ex) { return new ApiResponse<ExpenseRecordDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse> DeleteAsync(Guid id)
    {
        try { SetAuth(); return await ReadEmpty(await httpClient.DeleteAsync($"api/expense-records/{id}")); }
        catch (Exception ex) { return new ApiResponse { Success = false, Message = ex.Message }; }
    }
}
