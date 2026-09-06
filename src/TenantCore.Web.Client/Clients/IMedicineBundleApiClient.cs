using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Web.Client.Clients;

public interface IMedicineBundleApiClient
{
    Task<ApiResponse<IEnumerable<MedicineBundleDto>>> GetAllAsync();
    Task<ApiResponse<MedicineBundleDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<MedicineBundleDto>> CreateAsync(CreateMedicineBundleDto dto);
    Task<ApiResponse<MedicineBundleDto>> UpdateAsync(Guid id, UpdateMedicineBundleDto dto);
    Task<ApiResponse<bool>> DeleteAsync(Guid id);
}
