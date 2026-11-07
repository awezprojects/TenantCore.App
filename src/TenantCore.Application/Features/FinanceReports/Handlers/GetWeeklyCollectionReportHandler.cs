using MediatR;
using TenantCore.Application.Features.FinanceReports.Queries;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.FinanceReports.Handlers;

public sealed class GetWeeklyCollectionReportHandler(
    IOpdPaymentRepository repository,
    IOpdParticularRepository particularRepository) : IRequestHandler<GetWeeklyCollectionReportQuery, WeeklyCollectionReportDto>
{
    public async Task<WeeklyCollectionReportDto> Handle(GetWeeklyCollectionReportQuery request, CancellationToken cancellationToken)
    {
        // DEF-E2E-10: AmountReceivedAt is stored in UTC. Query bounds shift by -offset to cover
        // the local week; per-day bucketing shifts each timestamp by +offset before taking .Date
        // so a local-day request lines up with local-day collections, not UTC ones.
        var offset = request.UtcOffsetMinutes;
        var weekStart = request.WeekStartDate.Date;
        var weekStartUtc = weekStart.AddMinutes(-offset);
        var weekEndUtc = weekStartUtc.AddDays(7).AddTicks(-1);

        var payments = (await repository.GetByDateRangeAsync(weekStartUtc, weekEndUtc, request.ApplicationId, cancellationToken)).ToList();

        // DEF-E2E-12: a payment's CollectedAmount is a running total across every collection event
        // for that registration, however many days apart — crediting it whole to the day the visit
        // fee was first received would also carry in items collected on a later day. Back the item
        // portion out ("base" = fee/full-bill only) and bucket items collected this week by their
        // own CollectedAt instead.
        var itemTotals = await particularRepository.GetIndividuallyCollectedTotalsByRegistrationIdsAsync(
            payments.Select(p => p.OpdRegistrationId), request.ApplicationId, cancellationToken);
        var itemsThisWeek = await particularRepository.GetCollectedByDateRangeAsync(weekStartUtc, weekStartUtc.AddDays(7), request.ApplicationId, cancellationToken);
        var itemsByDay = itemsThisWeek
            .GroupBy(i => i.CollectedAt!.Value.AddMinutes(offset).Date)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Amount));

        var dayTotals = Enumerable.Range(0, 7)
            .Select(i =>
            {
                var day = weekStart.AddDays(i);
                var baseAmount = payments
                    .Where(p => p.AmountReceivedAt.HasValue && p.AmountReceivedAt.Value.AddMinutes(offset).Date == day)
                    .Sum(p => p.CollectedAmount - itemTotals.GetValueOrDefault(p.OpdRegistrationId));
                var amount = baseAmount + itemsByDay.GetValueOrDefault(day);
                return new DailyTotalDto { Date = day, DayOfWeek = day.DayOfWeek.ToString(), Amount = amount };
            })
            .ToList();

        return new WeeklyCollectionReportDto
        {
            WeekStart = weekStart,
            DayTotals = dayTotals,
            GrandTotal = dayTotals.Sum(d => d.Amount)
        };
    }
}
