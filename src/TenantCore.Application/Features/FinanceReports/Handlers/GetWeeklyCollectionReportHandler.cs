using MediatR;
using TenantCore.Application.Features.FinanceReports.Queries;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.FinanceReports.Handlers;

public sealed class GetWeeklyCollectionReportHandler(IOpdPaymentRepository repository) : IRequestHandler<GetWeeklyCollectionReportQuery, WeeklyCollectionReportDto>
{
    public async Task<WeeklyCollectionReportDto> Handle(GetWeeklyCollectionReportQuery request, CancellationToken cancellationToken)
    {
        var weekStart = request.WeekStartDate.Date;
        var weekEnd = weekStart.AddDays(7).AddTicks(-1);

        var payments = await repository.GetByDateRangeAsync(weekStart, weekEnd, request.ApplicationId, cancellationToken);

        var dayTotals = Enumerable.Range(0, 7)
            .Select(i =>
            {
                var day = weekStart.AddDays(i);
                var amount = payments
                    .Where(p => p.AmountReceivedAt.HasValue && p.AmountReceivedAt.Value.Date == day)
                    .Sum(p => p.FinalAmount);
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
