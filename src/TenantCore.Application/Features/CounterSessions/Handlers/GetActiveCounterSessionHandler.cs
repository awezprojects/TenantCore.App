using MediatR;
using TenantCore.Application.Features.CounterSessions.Queries;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.CounterSessions.Handlers;

public sealed class GetActiveCounterSessionHandler(
    ICounterSessionRepository repository,
    IOpdPaymentRepository paymentRepository,
    IOpdParticularRepository particularRepository,
    IExpenseRecordRepository expenseRecordRepository)
    : IRequestHandler<GetActiveCounterSessionQuery, CounterSessionDto?>
{
    public async Task<CounterSessionDto?> Handle(GetActiveCounterSessionQuery request, CancellationToken cancellationToken)
    {
        var session = await repository.GetActiveSessionAsync(request.ApplicationId, cancellationToken);
        if (session is null) return null;

        var payments = await paymentRepository.GetBySessionIdAsync(session.Id, session.ApplicationId, cancellationToken);
        var particulars = await particularRepository.GetCollectedBySessionIdAsync(session.Id, session.ApplicationId, cancellationToken);
        var liveCollected = payments.Sum(p => p.CollectedAmount) + particulars.Sum(p => p.Amount);

        var expenses = await expenseRecordRepository.GetBySessionIdAsync(session.Id, session.ApplicationId, cancellationToken);
        var liveExpenses = expenses.Sum(e => e.PaidAmount);

        return new CounterSessionDto
        {
            Id = session.Id,
            ApplicationId = session.ApplicationId,
            SessionDate = session.SessionDate,
            OpenedByUserId = session.OpenedByUserId,
            OpenedAt = session.OpenedAt,
            ClosedAt = session.ClosedAt,
            Status = session.Status,
            TotalCollected = liveCollected,
            TotalExpenses = liveExpenses,
            NetAmount = liveCollected - liveExpenses
        };
    }
}
