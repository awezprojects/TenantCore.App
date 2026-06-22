using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Web.Client.Clients;

public interface IParticularApiClient
{
    Task<ApiResponse<IEnumerable<ParticularSummaryDto>>> GetAllAsync();
    Task<ApiResponse<ParticularDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<Guid>> CreateAsync(CreateParticularRequest request);
    Task<ApiResponse<ParticularDto>> UpdateAsync(Guid id, UpdateParticularRequest request);
    Task<ApiResponse> DeleteAsync(Guid id);
}
