using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class ClinicUsgTemplateRepository(ClinicDbContext dbContext)
    : ClinicRepository<ClinicUsgTemplate>(dbContext), IClinicUsgTemplateRepository
{
    public async Task<ClinicUsgTemplate?> GetByApplicationIdAsync(Guid applicationId, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(t => t.ApplicationId == applicationId, ct);

    public async Task<ClinicUsgTemplate?> GetByApplicationIdWithRowsAsync(Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .Include(t => t.Rows.OrderBy(r => r.RowOrder))
            .FirstOrDefaultAsync(t => t.ApplicationId == applicationId, ct);

    public void RemoveRows(IEnumerable<UsgTemplateRow> rows)
        => DbContext.Set<UsgTemplateRow>().RemoveRange(rows);

    public async Task AddRowsAsync(IEnumerable<UsgTemplateRow> rows, CancellationToken ct = default)
        => await DbContext.Set<UsgTemplateRow>().AddRangeAsync(rows, ct);
}
