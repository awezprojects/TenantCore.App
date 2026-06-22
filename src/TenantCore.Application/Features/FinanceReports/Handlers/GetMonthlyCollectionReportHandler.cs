using MediatR;
using TenantCore.Application.Features.FinanceReports.Queries;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.FinanceReports.Handlers;

public sealed class GetMonthlyCollectionReportHandler(IOpdPaymentRepository repository) : IRequestHandler<GetMonthlyCollectionReportQuery, MonthlyCollectionReportDto>
{
    public async Task<MonthlyCollectionReportDto> Handle(GetMonthlyCollectionReportQuery request, CancellationToken cancellationToken)
    {
        var monthStart = new DateTime(request.Year, request.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddTicks(-1);

        var payments = await repository.GetByDateRangeAsync(monthStart, monthEnd, request.ApplicationId, cancellationToken);
        var daysInMonth = DateTime.DaysInMonth(request.Year, request.Month);

        var dayTotals = Enumerable.Range(1, daysInMonth)
            .Select(d =>
            {
                var day = new DateTime(request.Year, request.Month, d);
                var amount = payments
                    .Where(p => p.AmountReceivedAt.HasValue && p.AmountReceivedAt.Value.Date == day)
                    .Sum(p => p.FinalAmount);
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
