using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IExpenseCategoryRepository : IClinicRepository<ExpenseCategory>
{
    Task<IEnumerable<ExpenseCategory>> GetActiveAsync(Guid applicationId, CancellationToken ct = default);
}
