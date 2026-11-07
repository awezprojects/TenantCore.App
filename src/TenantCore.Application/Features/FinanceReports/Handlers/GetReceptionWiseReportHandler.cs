using MediatR;
using TenantCore.Application.Features.FinanceReports.Queries;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.FinanceReports.Handlers;

public sealed class GetReceptionWiseReportHandler(
    ICounterSessionRepository counterSessionRepository,
    IOpdPaymentRepository opdPaymentRepository,
    IOpdParticularRepository opdParticularRepository,
    IExpenseRecordRepository expenseRecordRepository) : IRequestHandler<GetReceptionWiseReportQuery, IEnumerable<ReceptionWiseReportDto>>
{
    public async Task<IEnumerable<ReceptionWiseReportDto>> Handle(GetReceptionWiseReportQuery request, CancellationToken cancellationToken)
    {
        var offset = request.UtcOffsetMinutes;
        var fromUtc = request.From.Date.AddMinutes(-offset);
        var toUtc   = request.To.Date.AddDays(1).AddMinutes(-offset);

        var sessions = await counterSessionRepository.GetAllAsync(cancellationToken);
        var filteredSessions = sessions
            .Where(s => s.ApplicationId == request.ApplicationId
                     && s.OpenedAt >= fromUtc
                     && s.OpenedAt < toUtc)
            .ToList();

        var payments = (await opdPaymentRepository.GetByDateRangeAsync(fromUtc, toUtc, request.ApplicationId, cancellationToken)).ToList();
        var expenses = await expenseRecordRepository.GetByDateRangeAsync(fromUtc, toUtc, request.ApplicationId, cancellationToken);

        // DEF-E2E-12: a payment's CollectedAmount is a running total across every collection event
        // for that registration, however many sessions apart — crediting it whole to its home
        // session would also carry in items collected in a later session owned by this group's
        // sessions. Back the item portion out ("base" = fee/full-bill only) and credit items by
        // the session they were actually collected in instead.
        var itemTotals = await opdParticularRepository.GetIndividuallyCollectedTotalsByRegistrationIdsAsync(
            payments.Select(p => p.OpdRegistrationId), request.ApplicationId, cancellationToken);
        var allSessionIds = filteredSessions.Select(s => s.Id).ToList();
        var itemsBySessionId = (await opdParticularRepository.GetCollectedBySessionIdsAsync(allSessionIds, request.ApplicationId, cancellationToken))
            .GroupBy(i => i.CounterSessionId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Amount));

        return filteredSessions
            .GroupBy(s => s.OpenedByUserId)
            .Select(g =>
            {
                var sessionIds = g.Select(s => s.Id).ToHashSet();
                var baseCollected = payments
                    .Where(p => p.CounterSessionId.HasValue && sessionIds.Contains(p.CounterSessionId.Value))
                    .Sum(p => p.CollectedAmount - itemTotals.GetValueOrDefault(p.OpdRegistrationId));
                var itemsCollected = sessionIds.Sum(id => itemsBySessionId.GetValueOrDefault(id));
                var expensed = expenses.Where(e => e.CounterSessionId.HasValue && sessionIds.Contains(e.CounterSessionId.Value)).Sum(e => e.Amount);
                return new ReceptionWiseReportDto
                {
                    UserId = g.Key,
                    TotalSessions = g.Count(),
                    TotalCollected = baseCollected + itemsCollected,
                    TotalExpenses = expensed
                };
            });
    }
}
