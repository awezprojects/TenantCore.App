using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Repositories;

public class AmountHandoverRepository(ClinicDbContext dbContext) : ClinicRepository<AmountHandover>(dbContext), IAmountHandoverRepository
{
    public async Task<IEnumerable<AmountHandover>> GetPendingAsync(Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .Where(a => a.ApplicationId == applicationId && a.Status == HandoverStatus.Pending)
            .OrderByDescending(a => a.HandedOverAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<AmountHandover>> GetBySessionIdAsync(Guid sessionId, Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .Where(a => a.CounterSessionId == sessionId && a.ApplicationId == applicationId)
            .OrderByDescending(a => a.HandedOverAt)
            .ToListAsync(ct);

    public async Task<bool> HasAcceptedHandoverAsync(Guid sessionId, Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AnyAsync(a => a.CounterSessionId == sessionId
                        && a.ApplicationId == applicationId
                        && a.Status == HandoverStatus.Accepted, ct);

    public async Task<IEnumerable<AmountHandover>> GetByDateRangeAsync(Guid applicationId, DateTime? fromInclusive, DateTime? toExclusive, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .Where(a => a.ApplicationId == applicationId
                && (fromInclusive == null || a.HandedOverAt >= fromInclusive.Value)
                && (toExclusive   == null || a.HandedOverAt <  toExclusive.Value))
            .OrderByDescending(a => a.HandedOverAt)
            .ToListAsync(ct);
}
