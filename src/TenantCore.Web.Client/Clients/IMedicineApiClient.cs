using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Web.Client.Clients;

public interface IMedicineApiClient
{
    Task<ApiResponse<PagedResult<MedicineTypeDto>>> GetMedicineTypesAsync(int page = 1, int pageSize = 50, string? search = null);
    Task<ApiResponse<MedicineTypeDto>> CreateMedicineTypeAsync(CreateMedicineTypeDto dto);
    Task<ApiResponse<MedicineTypeDto>> UpdateMedicineTypeAsync(Guid id, UpdateMedicineTypeDto dto);

    Task<ApiResponse<PagedResult<MedicineDto>>> GetMedicinesAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        string? brandName = null,
        string? genericName = null,
        Guid? medicineTypeId = null,
        bool? isGeneric = null);
    Task<ApiResponse<MedicineDto>> GetMedicineByIdAsync(Guid id);
    Task<ApiResponse<MedicineDto>> CreateMedicineAsync(CreateMedicineDto dto);
    Task<ApiResponse<MedicineDto>> UpdateMedicineAsync(Guid id, UpdateMedicineDto dto);
}
