using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Repositories;

public class OpdPaymentRepository(ClinicDbContext dbContext) : ClinicRepository<OpdPayment>(dbContext), IOpdPaymentRepository
{
    public async Task<OpdPayment?> GetByOpdRegistrationIdAsync(Guid opdRegistrationId, Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .FirstOrDefaultAsync(o => o.OpdRegistrationId == opdRegistrationId && o.ApplicationId == applicationId, ct);

    public async Task<IEnumerable<OpdPayment>> GetBySessionIdAsync(Guid sessionId, Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .Where(o => o.CounterSessionId == sessionId
                     && o.ApplicationId == applicationId
                     && o.PaymentStatus == PaymentStatus.Received)
            .ToListAsync(ct);

    public async Task<IEnumerable<OpdPayment>> GetByDateRangeAsync(DateTime from, DateTime to, Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .Where(o => o.ApplicationId == applicationId
                     && o.PaymentStatus == PaymentStatus.Received
                     && o.AmountReceivedAt >= from
                     && o.AmountReceivedAt <= to)
            .ToListAsync(ct);

    public async Task<IEnumerable<OpdPayment>> GetByOpdRegistrationIdsAsync(IEnumerable<Guid> opdRegistrationIds, Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .Where(o => o.ApplicationId == applicationId && opdRegistrationIds.Contains(o.OpdRegistrationId))
            .ToListAsync(ct);

    public async Task<IEnumerable<(Guid DoctorUserId, string DoctorName, decimal Total)>> GetDoctorWiseTotalsAsync(
        DateTime from, DateTime to, Guid applicationId, Guid? doctorUserId, CancellationToken ct = default)
    {
        var query = dbContext.Set<OpdPayment>()
            .AsNoTracking()
            .Where(o => o.ApplicationId == applicationId
                     && o.PaymentStatus == PaymentStatus.Received
                     && o.AmountReceivedAt >= from
                     && o.AmountReceivedAt < to)
            .Join(dbContext.Set<OpdRegistration>(),
                  p => p.OpdRegistrationId,
                  r => r.Id,
                  (p, r) => new { p.FinalAmount, r.DoctorUserId, r.DoctorName });

        if (doctorUserId.HasValue)
            query = query.Where(x => x.DoctorUserId == doctorUserId.Value);

        var grouped = await query
            .GroupBy(x => new { x.DoctorUserId, x.DoctorName })
            .Select(g => new { g.Key.DoctorUserId, g.Key.DoctorName, Total = g.Sum(x => x.FinalAmount) })
            .OrderByDescending(x => x.Total)
            .ToListAsync(ct);

        return grouped.Select(x => (x.DoctorUserId, x.DoctorName, x.Total));
    }
}
