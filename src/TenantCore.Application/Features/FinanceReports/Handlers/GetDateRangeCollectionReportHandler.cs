using MediatR;
using TenantCore.Application.Features.FinanceReports.Queries;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.FinanceReports.Handlers;

public sealed class GetDateRangeCollectionReportHandler(IOpdPaymentRepository repository) : IRequestHandler<GetDateRangeCollectionReportQuery, DateRangeCollectionReportDto>
{
    public async Task<DateRangeCollectionReportDto> Handle(GetDateRangeCollectionReportQuery request, CancellationToken cancellationToken)
    {
        var offset = request.UtcOffsetMinutes;
        var fromUtc = request.From.Date.AddMinutes(-offset);
        var toUtc   = request.To.Date.AddDays(1).AddMinutes(-offset);
        var payments = await repository.GetByDateRangeAsync(fromUtc, toUtc, request.ApplicationId, cancellationToken);

        var dayTotals = payments
            .Where(p => p.AmountReceivedAt.HasValue)
            .GroupBy(p => p.AmountReceivedAt!.Value.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailyTotalDto
            {
                Date = g.Key,
                DayOfWeek = g.Key.DayOfWeek.ToString(),
                Amount = g.Sum(p => p.FinalAmount)
            })
            .ToList();

        return new DateRangeCollectionReportDto
        {
            From = request.From,
            To = request.To,
            DayTotals = dayTotals,
            GrandTotal = dayTotals.Sum(d => d.Amount)
        };
    }
}
