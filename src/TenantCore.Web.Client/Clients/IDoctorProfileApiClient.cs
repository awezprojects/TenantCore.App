using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Web.Client.Clients;

public interface IDoctorProfileApiClient
{
    Task<ApiResponse<DoctorProfileDto>> GetMyProfileAsync();
    Task<ApiResponse<DoctorProfileDto>> UpsertMyProfileAsync(UpsertDoctorProfileDto dto);
}
