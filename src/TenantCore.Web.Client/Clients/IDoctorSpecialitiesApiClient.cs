using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Web.Client.Clients;

public interface IDoctorSpecialitiesApiClient
{
    Task<ApiResponse<List<DoctorSpecialityDto>>> GetAllAsync();
}
