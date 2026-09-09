using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Web.Client.Clients;

public interface IVitalPresetApiClient
{
    Task<ApiResponse<IEnumerable<VitalPresetLookupItemDto>>> GetAllAsync();
    Task<ApiResponse<VitalPresetLookupItemDto>> AddAsync(AddVitalPresetLookupItemDto dto);
    Task<ApiResponse> DeleteAsync(Guid id);
}
