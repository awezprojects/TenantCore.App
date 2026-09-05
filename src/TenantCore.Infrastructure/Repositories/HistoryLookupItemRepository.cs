using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Repositories;

public class HistoryLookupItemRepository(ClinicDbContext dbContext)
    : ClinicRepository<HistoryLookupItem>(dbContext), IHistoryLookupItemRepository
{
    public async Task<List<HistoryLookupItem>> GetForApplicationAsync(Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .Where(h => h.ApplicationId == null || h.ApplicationId == applicationId)
            .OrderBy(h => h.Type).ThenBy(h => h.Value)
            .ToListAsync(ct);

    public async Task<HistoryLookupItem?> FindAsync(HistoryItemType type, Guid applicationId, string value, CancellationToken ct = default)
        => await DbSet
            .Where(h => h.Type == type && (h.ApplicationId == null || h.ApplicationId == applicationId))
            .FirstOrDefaultAsync(h => h.Value.ToLower() == value.ToLower(), ct);
}
