using MediatR;
using TenantCore.Application.Features.CounterSessions.Queries;
using TenantCore.Application.Features.CounterSessions.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.CounterSessions.Handlers;

public sealed class GetAllActiveCounterSessionsHandler(
    ICounterSessionRepository repository,
    IOpdPaymentRepository paymentRepository,
    IOpdParticularRepository particularRepository,
    IExpenseRecordRepository expenseRecordRepository)
    : IRequestHandler<GetAllActiveCounterSessionsQuery, IEnumerable<CounterSessionSummaryDto>>
{
    public async Task<IEnumerable<CounterSessionSummaryDto>> Handle(
        GetAllActiveCounterSessionsQuery request, CancellationToken cancellationToken)
    {
        var sessions = await repository.GetAllActiveAsync(request.ApplicationId, cancellationToken);
        if (sessions.Count == 0) return [];

        var result = new List<CounterSessionSummaryDto>(sessions.Count);
        foreach (var session in sessions)
        {
            var payments    = await paymentRepository.GetBySessionIdAsync(session.Id, session.ApplicationId, cancellationToken);
            var particulars = await particularRepository.GetCollectedBySessionIdAsync(session.Id, session.ApplicationId, cancellationToken);
            var expenses    = await expenseRecordRepository.GetBySessionIdAsync(session.Id, session.ApplicationId, cancellationToken);

            var collected = payments.Sum(p => p.CollectedAmount) + particulars.Sum(p => p.Amount);
            var exp       = expenses.Sum(e => e.PaidAmount);

            result.Add(CounterSessionTranslator.ToSummaryDto(session, collected, exp));
        }
        return result;
    }
}
