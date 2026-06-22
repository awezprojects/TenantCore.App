using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Web.Client.Clients;

public interface IAmountHandoverApiClient
{
    Task<ApiResponse<IEnumerable<AmountHandoverSummaryDto>>> GetAllAsync(DateTime? from = null, DateTime? to = null);
    Task<ApiResponse<IEnumerable<AmountHandoverSummaryDto>>> GetPendingAsync();
    Task<ApiResponse<IEnumerable<AmountHandoverSummaryDto>>> GetBySessionAsync(Guid counterSessionId);
    Task<ApiResponse<AmountHandoverDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<Guid>> CreateAsync(CreateAmountHandoverRequest request);
    Task<ApiResponse<Guid>> CollectAsync(CollectAmountHandoverRequest request);
    Task<ApiResponse<AmountHandoverDto>> AcceptAsync(Guid id);
    Task<ApiResponse<AmountHandoverDto>> DisputeAsync(Guid id, ResolveAmountHandoverRequest request);
}
