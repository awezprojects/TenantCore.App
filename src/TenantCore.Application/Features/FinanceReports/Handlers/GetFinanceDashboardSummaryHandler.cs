using MediatR;
using TenantCore.Application.Features.FinanceReports.Queries;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.FinanceReports.Handlers;

public sealed class GetFinanceDashboardSummaryHandler(
    IOpdPaymentRepository opdPaymentRepository,
    IOpdParticularRepository opdParticularRepository,
    IExpenseRecordRepository expenseRecordRepository,
    ICounterSessionRepository counterSessionRepository) : IRequestHandler<GetFinanceDashboardSummaryQuery, FinanceDashboardSummaryDto>
{
    public async Task<FinanceDashboardSummaryDto> Handle(GetFinanceDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        // DEF-E2E-10: AmountReceivedAt is stored in UTC; shift the day boundary by the caller's
        // utcOffset so a local-day request lines up with local-day collections, not UTC ones.
        var offset = request.UtcOffsetMinutes;
        var dayStart = request.Date.Date.AddMinutes(-offset);
        var dayEnd = dayStart.AddDays(1).AddTicks(-1);

        var payments = (await opdPaymentRepository.GetByDateRangeAsync(dayStart, dayEnd, request.ApplicationId, cancellationToken)).ToList();
        var expenses = await expenseRecordRepository.GetByDateRangeAsync(dayStart, dayEnd, request.ApplicationId, cancellationToken);
        var activeSession = await counterSessionRepository.GetActiveSessionAsync(request.ApplicationId, cancellationToken);

        // DEF-E2E-12: a payment's CollectedAmount is a running total across every collection event
        // for that registration, however many days apart — crediting it whole to the day the visit
        // fee was first received would also carry in items collected on a later day. Back the item
        // portion out ("base" = fee/full-bill only) and add items collected today by their own
        // CollectedAt instead.
        var itemTotals = await opdParticularRepository.GetIndividuallyCollectedTotalsByRegistrationIdsAsync(
            payments.Select(p => p.OpdRegistrationId), request.ApplicationId, cancellationToken);
        var itemsCollectedToday = (await opdParticularRepository.GetCollectedByDateRangeAsync(dayStart, dayStart.AddDays(1), request.ApplicationId, cancellationToken)).Sum(i => i.Amount);
        var totalCollected = payments.Sum(p => p.CollectedAmount - itemTotals.GetValueOrDefault(p.OpdRegistrationId)) + itemsCollectedToday;
        var totalExpenses = expenses.Sum(e => e.Amount);

        return new FinanceDashboardSummaryDto
        {
            TotalCollected = totalCollected,
            TotalExpenses = totalExpenses,
            NetAmount = totalCollected - totalExpenses,
            HasActiveSession = activeSession is not null,
            ActiveSessionId = activeSession?.Id,
            ActiveSessionOpenedAt = activeSession?.OpenedAt
        };
    }
}
