using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Web.Client.Clients;

public interface IExpenseCategoryApiClient
{
    Task<ApiResponse<IEnumerable<ExpenseCategorySummaryDto>>> GetAllAsync();
    Task<ApiResponse<ExpenseCategoryDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<Guid>> CreateAsync(CreateExpenseCategoryRequest request);
    Task<ApiResponse<ExpenseCategoryDto>> UpdateAsync(Guid id, UpdateExpenseCategoryRequest request);
    Task<ApiResponse> DeleteAsync(Guid id);
}
