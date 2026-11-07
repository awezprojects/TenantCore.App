using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Repositories;

public class OpdParticularRepository(ClinicDbContext dbContext) : ClinicRepository<OpdParticular>(dbContext), IOpdParticularRepository
{
    public async Task<IEnumerable<OpdParticular>> GetByOpdRegistrationIdAsync(Guid opdRegistrationId, Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .Where(o => o.OpdRegistrationId == opdRegistrationId && o.ApplicationId == applicationId)
            .ToListAsync(ct);

    public async Task<decimal> GetTotalByOpdRegistrationIdAsync(Guid opdRegistrationId, Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .Where(o => o.OpdRegistrationId == opdRegistrationId && o.ApplicationId == applicationId)
            .SumAsync(o => o.Amount, ct);

    public async Task<IEnumerable<OpdParticular>> GetCollectedBySessionIdAsync(Guid sessionId, Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .Where(o => o.CounterSessionId == sessionId
                     && o.ApplicationId == applicationId
                     && o.PaymentStatus == PaymentStatus.Received)
            .ToListAsync(ct);

    public async Task<IEnumerable<OpdParticular>> GetCollectedBySessionIdsAsync(IEnumerable<Guid> sessionIds, Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .Where(o => o.CounterSessionId != null
                     && sessionIds.Contains(o.CounterSessionId!.Value)
                     && o.ApplicationId == applicationId
                     && o.PaymentStatus == PaymentStatus.Received)
            .ToListAsync(ct);

    public async Task<IEnumerable<OpdParticular>> GetCollectedByDateRangeAsync(DateTime fromUtc, DateTime toExclusiveUtc, Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .Where(o => o.ApplicationId == applicationId
                     && o.PaymentStatus == PaymentStatus.Received
                     && o.CounterSessionId != null
                     && o.CollectedAt >= fromUtc
                     && o.CollectedAt < toExclusiveUtc)
            .ToListAsync(ct);

    public async Task<Dictionary<Guid, decimal>> GetIndividuallyCollectedTotalsByRegistrationIdsAsync(IEnumerable<Guid> opdRegistrationIds, Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .Where(o => o.ApplicationId == applicationId
                     && o.PaymentStatus == PaymentStatus.Received
                     && o.CounterSessionId != null
                     && opdRegistrationIds.Contains(o.OpdRegistrationId))
            .GroupBy(o => o.OpdRegistrationId)
            .ToDictionaryAsync(g => g.Key, g => g.Sum(o => o.Amount), ct);
}
