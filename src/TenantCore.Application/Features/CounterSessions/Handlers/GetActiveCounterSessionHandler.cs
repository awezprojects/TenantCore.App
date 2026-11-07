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

        // DEF-E2E-08/11: payment.CollectedAmount is a running total across every collection event
        // for that registration, however many sessions apart — attributing it whole to this
        // session's payments would miss items collected in other sessions, or double-count items
        // collected here for a payment homed elsewhere. Back the item portion out of each payment
        // ("base" = fee/full-bill only), then add items by the session they were actually collected
        // in instead.
        var payments = await paymentRepository.GetBySessionIdAsync(session.Id, session.ApplicationId, cancellationToken);
        var itemTotals = await particularRepository.GetIndividuallyCollectedTotalsByRegistrationIdsAsync(
            payments.Select(p => p.OpdRegistrationId), session.ApplicationId, cancellationToken);
        var baseCollected = payments.Sum(p => p.CollectedAmount - itemTotals.GetValueOrDefault(p.OpdRegistrationId));
        var itemsCollectedHere = (await particularRepository.GetCollectedBySessionIdAsync(session.Id, session.ApplicationId, cancellationToken)).Sum(i => i.Amount);
        var liveCollected = baseCollected + itemsCollectedHere;

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
