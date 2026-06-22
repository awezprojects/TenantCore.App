using MediatR;
using TenantCore.Application.Features.FinanceReports.Queries;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.FinanceReports.Handlers;

public sealed class GetReceptionWiseReportHandler(
    ICounterSessionRepository counterSessionRepository,
    IOpdPaymentRepository opdPaymentRepository,
    IExpenseRecordRepository expenseRecordRepository) : IRequestHandler<GetReceptionWiseReportQuery, IEnumerable<ReceptionWiseReportDto>>
{
    public async Task<IEnumerable<ReceptionWiseReportDto>> Handle(GetReceptionWiseReportQuery request, CancellationToken cancellationToken)
    {
        var sessions = await counterSessionRepository.GetAllAsync(cancellationToken);
        var filteredSessions = sessions
            .Where(s => s.ApplicationId == request.ApplicationId
                     && s.OpenedAt >= request.From
                     && s.OpenedAt <= request.To)
            .ToList();

        var payments = await opdPaymentRepository.GetByDateRangeAsync(request.From, request.To, request.ApplicationId, cancellationToken);
        var expenses = await expenseRecordRepository.GetByDateRangeAsync(request.From, request.To, request.ApplicationId, cancellationToken);

        return filteredSessions
            .GroupBy(s => s.OpenedByUserId)
            .Select(g =>
            {
                var sessionIds = g.Select(s => s.Id).ToHashSet();
                var collected = payments.Where(p => p.CounterSessionId.HasValue && sessionIds.Contains(p.CounterSessionId.Value)).Sum(p => p.FinalAmount);
                var expensed = expenses.Where(e => e.CounterSessionId.HasValue && sessionIds.Contains(e.CounterSessionId.Value)).Sum(e => e.Amount);
                return new ReceptionWiseReportDto
                {
                    UserId = g.Key,
                    TotalSessions = g.Count(),
                    TotalCollected = collected,
                    TotalExpenses = expensed
                };
            });
    }
}
