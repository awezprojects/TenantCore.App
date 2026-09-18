using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;
using TenantCore.Web.Client.Services;

namespace TenantCore.Web.Client.Clients;

public class OpdPaymentApiClient(HttpClient httpClient, AuthStateService authState) : IOpdPaymentApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private void SetAuth() =>
        httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(authState.AccessToken)
            ? null
            : new AuthenticationHeaderValue("Bearer", authState.AccessToken);

    private static Task<ApiResponse<T>> Read<T>(HttpResponseMessage response)
        => ApiResponseReader.ReadAsync<T>(response);

    public async Task<ApiResponse<OpdPaymentDto>> GetByOpdAsync(Guid opdRegistrationId)
    {
        try { SetAuth(); return await Read<OpdPaymentDto>(await httpClient.GetAsync($"api/opd-payments/by-opd/{opdRegistrationId}")); }
        catch (Exception ex) { return new ApiResponse<OpdPaymentDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<IEnumerable<SessionCollectionDto>>> GetBySessionAsync(Guid sessionId)
    {
        try { SetAuth(); return await Read<IEnumerable<SessionCollectionDto>>(await httpClient.GetAsync($"api/opd-payments/by-session/{sessionId}")); }
        catch (Exception ex) { return new ApiResponse<IEnumerable<SessionCollectionDto>> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<Guid>> EnsureAsync(EnsureOpdPaymentRequest request)
    {
        try { SetAuth(); return await Read<Guid>(await httpClient.PostAsJsonAsync("api/opd-payments/ensure", request, JsonOptions)); }
        catch (Exception ex) { return new ApiResponse<Guid> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<OpdPaymentDto>> AcceptAsync(Guid id, AcceptOpdPaymentRequest request)
    {
        try { SetAuth(); return await Read<OpdPaymentDto>(await httpClient.PostAsJsonAsync($"api/opd-payments/{id}/accept", request, JsonOptions)); }
        catch (Exception ex) { return new ApiResponse<OpdPaymentDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<OpdPaymentDto>> ApplyDiscountAsync(Guid id, ApplyOpdDiscountRequest request)
    {
        try { SetAuth(); return await Read<OpdPaymentDto>(await httpClient.PostAsJsonAsync($"api/opd-payments/{id}/discount", request, JsonOptions)); }
        catch (Exception ex) { return new ApiResponse<OpdPaymentDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<OpdPaymentDto>> AcceptFullAsync(Guid id, AcceptOpdPaymentFullRequest request)
    {
        try { SetAuth(); return await Read<OpdPaymentDto>(await httpClient.PostAsJsonAsync($"api/opd-payments/{id}/accept-full", request, JsonOptions)); }
        catch (Exception ex) { return new ApiResponse<OpdPaymentDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<OpdPaymentDto>> ProcessRefundAsync(Guid id, ProcessOpdRefundRequest request)
    {
        try { SetAuth(); return await Read<OpdPaymentDto>(await httpClient.PostAsJsonAsync($"api/opd-payments/{id}/refund", request, JsonOptions)); }
        catch (Exception ex) { return new ApiResponse<OpdPaymentDto> { Success = false, Message = ex.Message }; }
    }
}
