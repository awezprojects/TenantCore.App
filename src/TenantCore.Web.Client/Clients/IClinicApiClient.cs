using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Web.Client.Clients;

public interface IClinicApiClient
{
    Task<ApiResponse<PagedResult<PatientDto>>> GetPatientsAsync(int page = 1, int pageSize = 20, string? search = null);
    Task<ApiResponse<PatientDto>> GetPatientByIdAsync(Guid id);
    Task<ApiResponse<PatientDto>> CreatePatientAsync(CreatePatientDto dto);
    Task<ApiResponse<PatientDto>> UpdatePatientAsync(Guid id, UpdatePatientDto dto);

    Task<ApiResponse<PagedResult<OpdRegistrationDto>>> GetOpdRegistrationsAsync(int page = 1, int pageSize = 20, string? search = null);
    Task<ApiResponse<OpdRegistrationDto>> GetOpdRegistrationByIdAsync(Guid id);
    Task<ApiResponse<OpdRegistrationDto>> CreateOpdRegistrationAsync(CreateOpdRegistrationDto dto);
    Task<ApiResponse<OpdRegistrationDto>> UpdateOpdRegistrationAsync(Guid id, UpdateOpdRegistrationDto dto);

    Task<ApiResponse<PagedResult<IpdRegistrationDto>>> GetIpdRegistrationsAsync(int page = 1, int pageSize = 20, string? search = null);
    Task<ApiResponse<IpdRegistrationDto>> GetIpdRegistrationByIdAsync(Guid id);
    Task<ApiResponse<IpdRegistrationDto>> CreateIpdRegistrationAsync(CreateIpdRegistrationDto dto);
    Task<ApiResponse<IpdRegistrationDto>> UpdateIpdRegistrationAsync(Guid id, UpdateIpdRegistrationDto dto);
    Task<ApiResponse<IpdRegistrationDto>> DischargePatientAsync(Guid id, DischargePatientDto dto);

    Task<ApiResponse<ClinicFeeConfigDto>> GetFeesAsync();
    Task<ApiResponse<ClinicFeeConfigDto>> UpdateFeesAsync(UpdateClinicFeeConfigDto dto);
    Task<ApiResponse<IEnumerable<DoctorDto>>> GetDoctorsAsync();
}
