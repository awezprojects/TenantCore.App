using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;
using TenantCore.Web.Client.Services;

namespace TenantCore.Web.Client.Clients;

public class ExpenseCategoryApiClient(HttpClient httpClient, AuthStateService authState) : IExpenseCategoryApiClient
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

    public async Task<ApiResponse<IEnumerable<ExpenseCategorySummaryDto>>> GetAllAsync()
    {
        try { SetAuth(); return await Read<IEnumerable<ExpenseCategorySummaryDto>>(await httpClient.GetAsync("api/expense-categories")); }
        catch (Exception ex) { return new ApiResponse<IEnumerable<ExpenseCategorySummaryDto>> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<ExpenseCategoryDto>> GetByIdAsync(Guid id)
    {
        try { SetAuth(); return await Read<ExpenseCategoryDto>(await httpClient.GetAsync($"api/expense-categories/{id}")); }
        catch (Exception ex) { return new ApiResponse<ExpenseCategoryDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<Guid>> CreateAsync(CreateExpenseCategoryRequest request)
    {
        try { SetAuth(); return await Read<Guid>(await httpClient.PostAsJsonAsync("api/expense-categories", request, JsonOptions)); }
        catch (Exception ex) { return new ApiResponse<Guid> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<ExpenseCategoryDto>> UpdateAsync(Guid id, UpdateExpenseCategoryRequest request)
    {
        try { SetAuth(); return await Read<ExpenseCategoryDto>(await httpClient.PutAsJsonAsync($"api/expense-categories/{id}", request, JsonOptions)); }
        catch (Exception ex) { return new ApiResponse<ExpenseCategoryDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse> DeleteAsync(Guid id)
    {
        try { SetAuth(); return await ReadEmpty(await httpClient.DeleteAsync($"api/expense-categories/{id}")); }
        catch (Exception ex) { return new ApiResponse { Success = false, Message = ex.Message }; }
    }
}
