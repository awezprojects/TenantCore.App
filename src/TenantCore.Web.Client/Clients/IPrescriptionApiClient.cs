using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;
using TenantCore.Shared.Enums;

namespace TenantCore.Web.Client.Clients;

public interface IPrescriptionApiClient
{
    Task<ApiResponse<PagedResult<PrescriptionDto>>> GetPrescriptionsAsync(
        int page = 1, int pageSize = 20, string? search = null,
        Guid? doctorUserId = null, Guid? patientId = null,
        DateTime? from = null, DateTime? to = null,
        Guid? applicationId = null);

    Task<ApiResponse<PrescriptionDto>> GetPrescriptionByIdAsync(Guid id);
    Task<ApiResponse<PrescriptionDto>> GetPrescriptionByOpdIdAsync(Guid opdRegistrationId);
    Task<ApiResponse<PrescriptionDto>> CreatePrescriptionAsync(CreatePrescriptionDto dto);
    Task<ApiResponse<PrescriptionDto>> UpdatePrescriptionAsync(Guid id, UpdatePrescriptionDto dto);
    Task<ApiResponse<PrescriptionDto>> SubmitPrescriptionAsync(Guid id);
    Task<ApiResponse<PrescriptionReportDto>> UploadReportAsync(Guid prescriptionId, Stream fileStream, string fileName);

    Task<ApiResponse<PagedResult<DosageRemarkDto>>> GetDosageRemarksAsync(
        int page = 1, int pageSize = 50, MedicineFormType? form = null);
    Task<ApiResponse<DosageRemarkDto>> CreateDosageRemarkAsync(CreateDosageRemarkDto dto);
    Task<ApiResponse<DosageRemarkDto>> UpdateDosageRemarkAsync(Guid id, UpdateDosageRemarkDto dto);
    Task<ApiResponse<bool>> DeleteDosageRemarkAsync(Guid id);

    Task<ApiResponse<PrescriptionConfigDto>> GetPrescriptionConfigAsync();
    Task<ApiResponse<PrescriptionConfigDto>> UpdatePrescriptionConfigAsync(UpdatePrescriptionConfigDto dto);
}
