using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class IpdPaymentRepository(ClinicDbContext dbContext) : ClinicRepository<IpdPayment>(dbContext), IIpdPaymentRepository
{
    public async Task<IpdPayment?> GetByRegistrationAsync(Guid ipdRegistrationId, Guid applicationId, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(p => p.IpdRegistrationId == ipdRegistrationId && p.ApplicationId == applicationId, ct);

    public async Task<IEnumerable<IpdPayment>> GetByRegistrationIdsAsync(IEnumerable<Guid> ipdRegistrationIds, Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .Where(p => p.ApplicationId == applicationId && ipdRegistrationIds.Contains(p.IpdRegistrationId))
            .ToListAsync(ct);

    public async Task<IEnumerable<IpdPayment>> GetSettledOrRefundedByDateRangeAsync(DateTime fromUtc, DateTime toUtc, Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .Where(p => p.ApplicationId == applicationId &&
                ((p.SettledAt != null && p.SettledAt >= fromUtc && p.SettledAt < toUtc) ||
                 (p.RefundedAt != null && p.RefundedAt >= fromUtc && p.RefundedAt < toUtc)))
            .ToListAsync(ct);
}
