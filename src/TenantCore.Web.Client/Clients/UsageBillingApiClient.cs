using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TenantCore.Shared.Dtos.Auth;
using TenantCore.Shared.Dtos.UsageBilling;
using TenantCore.Web.Client.Services;

namespace TenantCore.Web.Client.Clients;

public class UsageBillingApiClient(HttpClient httpClient, AuthStateService authState) : IUsageBillingApiClient
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

    public async Task<ApiResponse<ClinicBillingSummaryDto>> GetSummaryAsync()
    {
        try { SetAuth(); return await Read<ClinicBillingSummaryDto>(await httpClient.GetAsync("api/usage-billing/summary")); }
        catch (Exception ex) { return new ApiResponse<ClinicBillingSummaryDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<ClinicWalletDto>> GetWalletAsync()
    {
        try { SetAuth(); return await Read<ClinicWalletDto>(await httpClient.GetAsync("api/usage-billing/wallet")); }
        catch (Exception ex) { return new ApiResponse<ClinicWalletDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<ClinicUsageDto>> GetUsageAsync(DateTime from, DateTime to)
    {
        try
        {
            SetAuth();

            // Round-trip format: a local time sent without its offset would be read as UTC server-side.
            var url = $"api/usage-billing/usage?from={Uri.EscapeDataString(from.ToUniversalTime().ToString("O"))}" +
                      $"&to={Uri.EscapeDataString(to.ToUniversalTime().ToString("O"))}";

            return await Read<ClinicUsageDto>(await httpClient.GetAsync(url));
        }
        catch (Exception ex) { return new ApiResponse<ClinicUsageDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<WalletTopUpPaymentDto>> CreateTopUpAsync(decimal amount)
    {
        try
        {
            SetAuth();
            var request = new CreateWalletTopUpRequest { Amount = amount };
            return await Read<WalletTopUpPaymentDto>(
                await httpClient.PostAsJsonAsync("api/usage-billing/wallet/top-up", request, JsonOptions));
        }
        catch (Exception ex) { return new ApiResponse<WalletTopUpPaymentDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse> CheckTopUpAsync(Guid paymentId)
    {
        try { SetAuth(); return await ReadVoid(await httpClient.PostAsync($"api/usage-billing/wallet/top-up/{paymentId}/check", null)); }
        catch (Exception ex) { return new ApiResponse { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<RegistrationFeePaymentDto?>> GetActiveRegistrationFeePaymentAsync()
    {
        try
        {
            SetAuth();
            var response = await httpClient.GetAsync("api/usage-billing/registration-fee/active");
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                return new ApiResponse<RegistrationFeePaymentDto?> { Success = true, Data = null };
            return await Read<RegistrationFeePaymentDto?>(response);
        }
        catch (Exception ex) { return new ApiResponse<RegistrationFeePaymentDto?> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<RegistrationFeePaymentDto>> CreateRegistrationFeePaymentAsync()
    {
        try { SetAuth(); return await Read<RegistrationFeePaymentDto>(await httpClient.PostAsync("api/usage-billing/registration-fee", null)); }
        catch (Exception ex) { return new ApiResponse<RegistrationFeePaymentDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse> CheckRegistrationFeePaymentAsync(Guid paymentId)
    {
        try { SetAuth(); return await ReadVoid(await httpClient.PostAsync($"api/usage-billing/registration-fee/{paymentId}/check", null)); }
        catch (Exception ex) { return new ApiResponse { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<byte[]>> ExportExcelAsync(ClinicUsageDto usage, string currency)
    {
        try
        {
            SetAuth();
            var request = new ExportUsageRequest { Data = usage, Currency = currency };
            var response = await httpClient.PostAsJsonAsync("api/usage-billing/usage/export/excel", request, JsonOptions);

            if (!response.IsSuccessStatusCode)
                return new ApiResponse<byte[]> { Success = false, Message = $"Export failed ({(int)response.StatusCode})." };

            var bytes = await response.Content.ReadAsByteArrayAsync();
            return new ApiResponse<byte[]> { Success = true, Data = bytes };
        }
        catch (Exception ex) { return new ApiResponse<byte[]> { Success = false, Message = ex.Message }; }
    }
}
