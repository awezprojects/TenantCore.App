using MediatR;
using TenantCore.Application.Features.FinanceReports.Queries;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.FinanceReports.Handlers;

public sealed class GetDateRangeCollectionReportHandler(
    IOpdPaymentRepository repository,
    IOpdParticularRepository particularRepository) : IRequestHandler<GetDateRangeCollectionReportQuery, DateRangeCollectionReportDto>
{
    public async Task<DateRangeCollectionReportDto> Handle(GetDateRangeCollectionReportQuery request, CancellationToken cancellationToken)
    {
        var offset = request.UtcOffsetMinutes;
        var fromUtc = request.From.Date.AddMinutes(-offset);
        var toUtc   = request.To.Date.AddDays(1).AddMinutes(-offset);
        var payments = (await repository.GetByDateRangeAsync(fromUtc, toUtc, request.ApplicationId, cancellationToken)).ToList();

        // DEF-E2E-12: a payment's CollectedAmount is a running total across every collection event
        // for that registration, however many days apart — crediting it whole to the day the visit
        // fee was first received would also carry in items collected on a later day. Back the item
        // portion out ("base" = fee/full-bill only) and bucket items collected in range by their
        // own CollectedAt instead.
        var itemTotals = await particularRepository.GetIndividuallyCollectedTotalsByRegistrationIdsAsync(
            payments.Select(p => p.OpdRegistrationId), request.ApplicationId, cancellationToken);
        var itemsInRange = await particularRepository.GetCollectedByDateRangeAsync(fromUtc, toUtc, request.ApplicationId, cancellationToken);

        // DEF-E2E-10: bucket by the local date (AmountReceivedAt + offset), not the raw UTC date —
        // the query bounds above were already offset-aware, but the per-day grouping key was not.
        var baseByDay = payments
            .Where(p => p.AmountReceivedAt.HasValue)
            .GroupBy(p => p.AmountReceivedAt!.Value.AddMinutes(offset).Date)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.CollectedAmount - itemTotals.GetValueOrDefault(p.OpdRegistrationId)));
        var itemsByDay = itemsInRange
            .GroupBy(i => i.CollectedAt!.Value.AddMinutes(offset).Date)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Amount));

        var dayTotals = baseByDay.Keys.Union(itemsByDay.Keys)
            .OrderBy(d => d)
            .Select(d => new DailyTotalDto
            {
                Date = d,
                DayOfWeek = d.DayOfWeek.ToString(),
                Amount = baseByDay.GetValueOrDefault(d) + itemsByDay.GetValueOrDefault(d)
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
