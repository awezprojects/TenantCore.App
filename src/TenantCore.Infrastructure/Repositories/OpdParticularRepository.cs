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
}
