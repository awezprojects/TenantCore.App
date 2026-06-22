using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Web.Client.Clients;

public interface IExpenseRecordApiClient
{
    Task<ApiResponse<IEnumerable<ExpenseRecordSummaryDto>>> GetAllAsync(DateTime? from = null, DateTime? to = null);
    Task<ApiResponse<IEnumerable<ExpenseRecordSummaryDto>>> GetBySessionAsync(Guid sessionId);
    Task<ApiResponse<ExpenseRecordDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<Guid>> CreateAsync(CreateExpenseRecordRequest request);
    Task<ApiResponse<ExpenseRecordDto>> PayAsync(Guid id, PayExpenseRecordRequest request);
    Task<ApiResponse<ExpenseRecordDto>> UpdateAsync(Guid id, UpdateExpenseRecordRequest request);
    Task<ApiResponse> DeleteAsync(Guid id);
}
