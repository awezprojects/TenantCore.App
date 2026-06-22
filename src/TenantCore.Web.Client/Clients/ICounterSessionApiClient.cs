using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Web.Client.Clients;

public interface ICounterSessionApiClient
{
    Task<ApiResponse<IEnumerable<CounterSessionSummaryDto>>> GetAllAsync(DateTime? from = null, DateTime? to = null);
    Task<ApiResponse<IEnumerable<CounterSessionSummaryDto>>> GetAllActiveAsync();
    Task<ApiResponse<CounterSessionDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<CounterSessionDto>> GetActiveAsync();
    Task<ApiResponse<Guid>> OpenAsync(OpenCounterSessionRequest request);
    Task<ApiResponse<CounterSessionDto>> CloseAsync(Guid id);
}
