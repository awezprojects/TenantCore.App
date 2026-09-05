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
    Task<ApiResponse<bool>> DeletePatientAsync(Guid id);
    Task<ApiResponse<string>> UploadPatientPhotoAsync(Guid id, Microsoft.AspNetCore.Components.Forms.IBrowserFile file);

    Task<ApiResponse<PagedResult<OpdRegistrationDto>>> GetOpdRegistrationsAsync(int page = 1, int pageSize = 20, string? search = null, Guid? doctorUserId = null, bool todayOnly = false, DateTime? fromDate = null, DateTime? toDate = null, TenantCore.Shared.Enums.OpdStatus? status = null, bool notVisited = false, Guid? applicationId = null);
    Task<ApiResponse<OpdRegistrationDto>> GetOpdRegistrationByIdAsync(Guid id);
    Task<ApiResponse<OpdRegistrationDto>> CreateOpdRegistrationAsync(CreateOpdRegistrationDto dto);
    Task<ApiResponse<OpdRegistrationDto>> UpdateOpdRegistrationAsync(Guid id, UpdateOpdRegistrationDto dto);
    Task<ApiResponse<bool>> DeleteOpdRegistrationAsync(Guid id);

    Task<ApiResponse<PagedResult<IpdRegistrationDto>>> GetIpdRegistrationsAsync(int page = 1, int pageSize = 20, string? search = null);
    Task<ApiResponse<IpdRegistrationDto>> GetIpdRegistrationByIdAsync(Guid id);
    Task<ApiResponse<IpdRegistrationDto>> CreateIpdRegistrationAsync(CreateIpdRegistrationDto dto);
    Task<ApiResponse<IpdRegistrationDto>> UpdateIpdRegistrationAsync(Guid id, UpdateIpdRegistrationDto dto);
    Task<ApiResponse<IpdRegistrationDto>> DischargePatientAsync(Guid id, DischargePatientDto dto);

    Task<ApiResponse<ClinicFeeConfigDto>> GetFeesAsync();
    Task<ApiResponse<ClinicFeeConfigDto>> UpdateFeesAsync(UpdateClinicFeeConfigDto dto);
    Task<ApiResponse<ClinicFeatureFlagsDto>> GetFeatureFlagsAsync();
    Task<ApiResponse<ClinicFeatureFlagsDto>> UpdateFeatureFlagsAsync(UpdateClinicFeatureFlagsDto dto);
    Task<ApiResponse<IEnumerable<DoctorDto>>> GetDoctorsAsync();
    Task<ApiResponse<int>> GetDoctorOpdCountAsync(Guid doctorUserId, CancellationToken ct = default);

    Task<ApiResponse<IEnumerable<StateDto>>> GetStatesAsync();
    Task<ApiResponse<IEnumerable<CityDto>>> GetCitiesByStateAsync(Guid stateId);
    Task<ApiResponse<ClinicLocationDto>> GetClinicLocationAsync();
    Task<ApiResponse<ClinicLocationDto>> UpsertClinicLocationAsync(UpsertClinicLocationDto dto);

    Task<ApiResponse<IEnumerable<WardDto>>> GetWardsAsync();
    Task<ApiResponse<WardDto>> CreateWardAsync(CreateWardDto dto);
    Task<ApiResponse<WardDto>> UpdateWardAsync(Guid id, UpdateWardDto dto);
    Task<ApiResponse<bool>> DeleteWardAsync(Guid id);

    Task<ApiResponse<IEnumerable<RoomDto>>> GetRoomsByWardAsync(Guid wardId);
    Task<ApiResponse<RoomDto>> CreateRoomAsync(CreateRoomDto dto);
    Task<ApiResponse<RoomDto>> UpdateRoomAsync(Guid id, UpdateRoomDto dto);
    Task<ApiResponse<bool>> DeleteRoomAsync(Guid id);

    Task<ApiResponse<IEnumerable<BedDto>>> GetBedsByRoomAsync(Guid roomId);
    Task<ApiResponse<IEnumerable<BedDto>>> GetAvailableBedsAsync(Guid? wardId = null);
    Task<ApiResponse<BedDto>> CreateBedAsync(CreateBedDto dto);
    Task<ApiResponse<bool>> DeleteBedAsync(Guid id);
}
