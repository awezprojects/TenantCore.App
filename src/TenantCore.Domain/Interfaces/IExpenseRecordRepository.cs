using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IExpenseRecordRepository : IClinicRepository<ExpenseRecord>
{
    Task<IEnumerable<ExpenseRecord>> GetByDateRangeAsync(DateTime? fromInclusive, DateTime? toExclusive, Guid applicationId, CancellationToken ct = default);
    Task<IEnumerable<ExpenseRecord>> GetBySessionIdAsync(Guid sessionId, Guid applicationId, CancellationToken ct = default);
}
