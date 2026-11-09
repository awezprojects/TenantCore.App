using MediatR;
using TenantCore.Application.Features.FinanceReports.Queries;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

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

        var filteredSessions = (await counterSessionRepository.GetByOpenedDateRangeAsync(
            fromUtc, toUtc, request.ApplicationId, cancellationToken)).ToList();

        var expenses = await expenseRecordRepository.GetByDateRangeAsync(fromUtc, toUtc, request.ApplicationId, cancellationToken);

        // DEF-FB-02: this used to pre-filter payments by AmountReceivedAt and then match them to a
        // session's id — a different predicate than the one that decides a session's own collected
        // total (CloseCounterSessionHandler / GetCounterSessionsHandler, which scope purely by
        // CounterSessionId, no date filter on the payment). Same session set, different totals —
        // "Collection by Staff" didn't reconcile with "Counter Sessions" for the same range. Compute
        // each session's collected total the exact same way those two already do: the stored
        // TotalCollected for a closed session (fixed at close time), computed live only for a still-
        // open one.
        var collectedBySessionId = new Dictionary<Guid, decimal>();
        foreach (var session in filteredSessions)
        {
            if (session.Status != CounterSessionStatus.Open)
            {
                collectedBySessionId[session.Id] = session.TotalCollected;
                continue;
            }

            var sessionPayments = await opdPaymentRepository.GetBySessionIdAsync(session.Id, request.ApplicationId, cancellationToken);
            var itemTotals = await opdParticularRepository.GetIndividuallyCollectedTotalsByRegistrationIdsAsync(
                sessionPayments.Select(p => p.OpdRegistrationId), request.ApplicationId, cancellationToken);
            var baseCollected = sessionPayments.Sum(p => p.CollectedAmount - itemTotals.GetValueOrDefault(p.OpdRegistrationId));
            var itemsCollectedHere = (await opdParticularRepository.GetCollectedBySessionIdAsync(session.Id, request.ApplicationId, cancellationToken)).Sum(i => i.Amount);
            collectedBySessionId[session.Id] = baseCollected + itemsCollectedHere;
        }

        return filteredSessions
            .GroupBy(s => s.OpenedByUserId)
            .Select(g =>
            {
                var sessionIds = g.Select(s => s.Id).ToHashSet();
                var expensed = expenses.Where(e => e.CounterSessionId.HasValue && sessionIds.Contains(e.CounterSessionId.Value)).Sum(e => e.Amount);
                return new ReceptionWiseReportDto
                {
                    UserId = g.Key,
                    TotalSessions = g.Count(),
                    TotalCollected = sessionIds.Sum(id => collectedBySessionId.GetValueOrDefault(id)),
                    TotalExpenses = expensed
                };
            });
    }
}
