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
        var paymentsQuery = dbContext.Set<OpdPayment>()
            .AsNoTracking()
            .Where(o => o.ApplicationId == applicationId
                     && o.PaymentStatus == PaymentStatus.Received
                     && o.AmountReceivedAt >= from
                     && o.AmountReceivedAt < to)
            .Join(dbContext.Set<OpdRegistration>(),
                  p => p.OpdRegistrationId,
                  r => r.Id,
                  (p, r) => new { p.OpdRegistrationId, p.CollectedAmount, r.DoctorUserId, r.DoctorName });

        if (doctorUserId.HasValue)
            paymentsQuery = paymentsQuery.Where(x => x.DoctorUserId == doctorUserId.Value);

        var payments = await paymentsQuery.ToListAsync(ct);

        // DEF-E2E-12: a payment's CollectedAmount is a running total across every collection event
        // for that registration, however many days apart — crediting it whole to the day the visit
        // fee was first received would also carry in items collected on a later day. Back the item
        // portion out ("base" = fee/full-bill only) and credit items by the day they were actually
        // collected instead.
        var registrationIds = payments.Select(p => p.OpdRegistrationId).ToList();
        var itemTotals = registrationIds.Count == 0
            ? []
            : await dbContext.Set<OpdParticular>()
                .AsNoTracking()
                .Where(o => o.ApplicationId == applicationId
                         && o.PaymentStatus == PaymentStatus.Received
                         && o.CounterSessionId != null
                         && registrationIds.Contains(o.OpdRegistrationId))
                .GroupBy(o => o.OpdRegistrationId)
                .ToDictionaryAsync(g => g.Key, g => g.Sum(o => o.Amount), ct);

        var itemsInRangeQuery = dbContext.Set<OpdParticular>()
            .AsNoTracking()
            .Where(o => o.ApplicationId == applicationId
                     && o.PaymentStatus == PaymentStatus.Received
                     && o.CounterSessionId != null
                     && o.CollectedAt >= from
                     && o.CollectedAt < to)
            .Join(dbContext.Set<OpdRegistration>(),
                  i => i.OpdRegistrationId,
                  r => r.Id,
                  (i, r) => new { i.Amount, r.DoctorUserId, r.DoctorName });

        if (doctorUserId.HasValue)
            itemsInRangeQuery = itemsInRangeQuery.Where(x => x.DoctorUserId == doctorUserId.Value);

        var itemsInRange = await itemsInRangeQuery.ToListAsync(ct);

        var baseByDoctor = payments
            .GroupBy(p => new { p.DoctorUserId, p.DoctorName })
            .ToDictionary(g => g.Key, g => g.Sum(p => p.CollectedAmount - itemTotals.GetValueOrDefault(p.OpdRegistrationId)));
        var itemsByDoctor = itemsInRange
            .GroupBy(i => new { i.DoctorUserId, i.DoctorName })
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Amount));

        return baseByDoctor.Keys.Union(itemsByDoctor.Keys)
            .Select(k => (k.DoctorUserId, k.DoctorName, Total: baseByDoctor.GetValueOrDefault(k) + itemsByDoctor.GetValueOrDefault(k)))
            .OrderByDescending(x => x.Total)
            .ToList();
    }
}
