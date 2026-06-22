using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class ExpenseRecordRepository(ClinicDbContext dbContext) : ClinicRepository<ExpenseRecord>(dbContext), IExpenseRecordRepository
{
    public async Task<IEnumerable<ExpenseRecord>> GetByDateRangeAsync(DateTime? fromInclusive, DateTime? toExclusive, Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .Where(e => e.ApplicationId == applicationId
                && (fromInclusive == null || e.RecordedAt >= fromInclusive.Value)
                && (toExclusive   == null || e.RecordedAt <  toExclusive.Value))
            .OrderByDescending(e => e.RecordedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<ExpenseRecord>> GetBySessionIdAsync(Guid sessionId, Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .Where(e => e.CounterSessionId == sessionId && e.ApplicationId == applicationId)
            .ToListAsync(ct);
}
