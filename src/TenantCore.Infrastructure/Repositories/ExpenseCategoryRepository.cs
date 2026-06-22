using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class ExpenseCategoryRepository(ClinicDbContext dbContext) : ClinicRepository<ExpenseCategory>(dbContext), IExpenseCategoryRepository
{
    public async Task<IEnumerable<ExpenseCategory>> GetActiveAsync(Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .Where(e => e.ApplicationId == applicationId && e.IsActive)
            .OrderBy(e => e.Name)
            .ToListAsync(ct);
}
