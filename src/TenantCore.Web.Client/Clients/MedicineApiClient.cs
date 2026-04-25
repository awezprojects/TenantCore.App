using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;
using TenantCore.Web.Client.Services;

namespace TenantCore.Web.Client.Clients;

public class MedicineApiClient(HttpClient httpClient, AuthStateService authState) : IMedicineApiClient
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

    // --- Medicine Types ---

    public async Task<ApiResponse<PagedResult<MedicineTypeDto>>> GetMedicineTypesAsync(
        int page = 1, int pageSize = 50, string? search = null)
    {
        try
        {
            SetAuth();
            var url = $"api/medicine-types?page={page}&pageSize={pageSize}" +
                      (search is not null ? $"&search={Uri.EscapeDataString(search)}" : "");
            return await Ok<PagedResult<MedicineTypeDto>>(await httpClient.GetAsync(url));
        }
        catch (Exception ex) { return Fail<PagedResult<MedicineTypeDto>>(ex.Message); }
    }

    public async Task<ApiResponse<MedicineTypeDto>> CreateMedicineTypeAsync(CreateMedicineTypeDto dto)
    {
        try
        {
            SetAuth();
            return await Ok<MedicineTypeDto>(await httpClient.PostAsJsonAsync("api/medicine-types", dto, JsonOptions));
        }
        catch (Exception ex) { return Fail<MedicineTypeDto>(ex.Message); }
    }

    public async Task<ApiResponse<MedicineTypeDto>> UpdateMedicineTypeAsync(Guid id, UpdateMedicineTypeDto dto)
    {
        try
        {
            SetAuth();
            return await Ok<MedicineTypeDto>(await httpClient.PutAsJsonAsync($"api/medicine-types/{id}", dto, JsonOptions));
        }
        catch (Exception ex) { return Fail<MedicineTypeDto>(ex.Message); }
    }

    // --- Medicines ---

    public async Task<ApiResponse<PagedResult<MedicineDto>>> GetMedicinesAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        string? brandName = null,
        string? genericName = null,
        Guid? medicineTypeId = null,
        bool? isGeneric = null)
    {
        try
        {
            SetAuth();
            var url = $"api/medicines?page={page}&pageSize={pageSize}";
            if (search is not null) url += $"&search={Uri.EscapeDataString(search)}";
            if (brandName is not null) url += $"&brandName={Uri.EscapeDataString(brandName)}";
            if (genericName is not null) url += $"&genericName={Uri.EscapeDataString(genericName)}";
            if (medicineTypeId.HasValue) url += $"&medicineTypeId={medicineTypeId}";
            if (isGeneric.HasValue) url += $"&isGeneric={isGeneric.Value.ToString().ToLower()}";
            return await Ok<PagedResult<MedicineDto>>(await httpClient.GetAsync(url));
        }
        catch (Exception ex) { return Fail<PagedResult<MedicineDto>>(ex.Message); }
    }

    public async Task<ApiResponse<MedicineDto>> GetMedicineByIdAsync(Guid id)
    {
        try
        {
            SetAuth();
            return await Ok<MedicineDto>(await httpClient.GetAsync($"api/medicines/{id}"));
        }
        catch (Exception ex) { return Fail<MedicineDto>(ex.Message); }
    }

    public async Task<ApiResponse<MedicineDto>> CreateMedicineAsync(CreateMedicineDto dto)
    {
        try
        {
            SetAuth();
            return await Ok<MedicineDto>(await httpClient.PostAsJsonAsync("api/medicines", dto, JsonOptions));
        }
        catch (Exception ex) { return Fail<MedicineDto>(ex.Message); }
    }

    public async Task<ApiResponse<MedicineDto>> UpdateMedicineAsync(Guid id, UpdateMedicineDto dto)
    {
        try
        {
            SetAuth();
            return await Ok<MedicineDto>(await httpClient.PutAsJsonAsync($"api/medicines/{id}", dto, JsonOptions));
        }
        catch (Exception ex) { return Fail<MedicineDto>(ex.Message); }
    }
}
