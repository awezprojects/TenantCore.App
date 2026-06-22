using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Web.Client.Clients;

public interface IDoctorFeeConfigApiClient
{
    Task<ApiResponse<IEnumerable<DoctorFeeConfigSummaryDto>>> GetAllAsync();
    Task<ApiResponse<DoctorFeeConfigDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<DoctorFeeConfigDto>> GetByDoctorIdAsync(Guid doctorProfileId);
    Task<ApiResponse<Guid>> CreateAsync(CreateDoctorFeeConfigRequest request);
    Task<ApiResponse<DoctorFeeConfigDto>> UpdateAsync(Guid id, UpdateDoctorFeeConfigRequest request);
    Task<ApiResponse> DeleteAsync(Guid id);
}
