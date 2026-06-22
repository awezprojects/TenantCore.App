using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Web.Client.Clients;

public interface IOpdParticularApiClient
{
    Task<ApiResponse<IEnumerable<OpdParticularDto>>> GetByOpdAsync(Guid opdRegistrationId);
    Task<ApiResponse<OpdParticularDto>> AddAsync(AddOpdParticularRequest request);
    Task<ApiResponse<OpdParticularDto>> UpdateAsync(Guid id, UpdateOpdParticularRequest request);
    Task<ApiResponse> RemoveAsync(Guid id);
    Task<ApiResponse> CollectAsync(Guid particularId, CollectOpdParticularRequest request);
    Task<ApiResponse> CollectAllAsync(Guid opdRegistrationId, CollectAllOpdParticularsRequest request);
}
