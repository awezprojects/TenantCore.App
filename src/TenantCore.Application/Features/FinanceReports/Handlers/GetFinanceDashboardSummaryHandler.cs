using MediatR;
using TenantCore.Application.Features.FinanceReports.Queries;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.FinanceReports.Handlers;

public sealed class GetFinanceDashboardSummaryHandler(
    IOpdPaymentRepository opdPaymentRepository,
    IExpenseRecordRepository expenseRecordRepository,
    ICounterSessionRepository counterSessionRepository) : IRequestHandler<GetFinanceDashboardSummaryQuery, FinanceDashboardSummaryDto>
{
    public async Task<FinanceDashboardSummaryDto> Handle(GetFinanceDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var dayStart = request.Date.Date;
        var dayEnd = dayStart.AddDays(1).AddTicks(-1);

        var payments = await opdPaymentRepository.GetByDateRangeAsync(dayStart, dayEnd, request.ApplicationId, cancellationToken);
        var expenses = await expenseRecordRepository.GetByDateRangeAsync(dayStart, dayEnd, request.ApplicationId, cancellationToken);
        var activeSession = await counterSessionRepository.GetActiveSessionAsync(request.ApplicationId, cancellationToken);

        var totalCollected = payments.Sum(p => p.FinalAmount);
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
