using MediatR;
using TenantCore.Application.Features.FinanceReports.Queries;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.FinanceReports.Handlers;

public sealed class GetMonthlyCollectionReportHandler(
    IOpdPaymentRepository repository,
    IOpdParticularRepository particularRepository) : IRequestHandler<GetMonthlyCollectionReportQuery, MonthlyCollectionReportDto>
{
    public async Task<MonthlyCollectionReportDto> Handle(GetMonthlyCollectionReportQuery request, CancellationToken cancellationToken)
    {
        // DEF-E2E-10: AmountReceivedAt is stored in UTC. Query bounds shift by -offset to cover
        // the local month; per-day bucketing shifts each timestamp by +offset before taking .Date
        // so a local-day request lines up with local-day collections, not UTC ones.
        var offset = request.UtcOffsetMinutes;
        var monthStart = new DateTime(request.Year, request.Month, 1);
        var monthStartUtc = monthStart.AddMinutes(-offset);
        var monthEndUtc = monthStartUtc.AddMonths(1).AddTicks(-1);

        var payments = (await repository.GetByDateRangeAsync(monthStartUtc, monthEndUtc, request.ApplicationId, cancellationToken)).ToList();
        var daysInMonth = DateTime.DaysInMonth(request.Year, request.Month);

        // DEF-E2E-12: a payment's CollectedAmount is a running total across every collection event
        // for that registration, however many days apart — crediting it whole to the day the visit
        // fee was first received would also carry in items collected on a later day. Back the item
        // portion out ("base" = fee/full-bill only) and bucket items collected this month by their
        // own CollectedAt instead.
        var itemTotals = await particularRepository.GetIndividuallyCollectedTotalsByRegistrationIdsAsync(
            payments.Select(p => p.OpdRegistrationId), request.ApplicationId, cancellationToken);
        var itemsThisMonth = await particularRepository.GetCollectedByDateRangeAsync(monthStartUtc, monthStartUtc.AddMonths(1), request.ApplicationId, cancellationToken);
        var itemsByDay = itemsThisMonth
            .GroupBy(i => i.CollectedAt!.Value.AddMinutes(offset).Date)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Amount));

        var dayTotals = Enumerable.Range(1, daysInMonth)
            .Select(d =>
            {
                var day = new DateTime(request.Year, request.Month, d);
                var baseAmount = payments
                    .Where(p => p.AmountReceivedAt.HasValue && p.AmountReceivedAt.Value.AddMinutes(offset).Date == day)
                    .Sum(p => p.CollectedAmount - itemTotals.GetValueOrDefault(p.OpdRegistrationId));
                var amount = baseAmount + itemsByDay.GetValueOrDefault(day);
                return new DailyTotalDto { Date = day, DayOfWeek = day.DayOfWeek.ToString(), Amount = amount };
            })
            .ToList();

        var weeklySubtotals = dayTotals
            .GroupBy(d => System.Globalization.ISOWeek.GetWeekOfYear(d.Date))
            .Select(g => new WeeklySubtotalDto { WeekNumber = g.Key, Amount = g.Sum(d => d.Amount) })
            .ToList();

        return new MonthlyCollectionReportDto
        {
            Year = request.Year,
            Month = request.Month,
            DayTotals = dayTotals,
            WeeklySubtotals = weeklySubtotals,
            GrandTotal = dayTotals.Sum(d => d.Amount)
        };
    }
}
