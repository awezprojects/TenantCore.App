using MediatR;
using TenantCore.Application.Features.CounterSessions.Commands;
using TenantCore.Application.Features.CounterSessions.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.CounterSessions.Handlers;

public sealed class CloseCounterSessionHandler(
    ICounterSessionRepository counterSessionRepository,
    IOpdPaymentRepository opdPaymentRepository,
    IOpdParticularRepository opdParticularRepository,
    IExpenseRecordRepository expenseRecordRepository,
    IAmountHandoverRepository amountHandoverRepository) : IRequestHandler<CloseCounterSessionCommand, CounterSessionDto>
{
    public async Task<CounterSessionDto> Handle(CloseCounterSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await counterSessionRepository.GetByIdAsync(request.Id, cancellationToken);
        if (session is null || session.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(CounterSession), request.Id);

        if (session.Status == CounterSessionStatus.Closed)
            throw new InvalidOperationException("This counter session is already closed.");

        var handovers = await amountHandoverRepository.GetBySessionIdAsync(request.Id, request.ApplicationId, cancellationToken);
        if (handovers.Any(h => h.Status == HandoverStatus.Pending))
            throw new InvalidOperationException("Cannot close session: there is a pending handover that must be accepted or resolved first.");
        if (!handovers.Any(h => h.Status == HandoverStatus.Accepted))
            throw new InvalidOperationException("Cannot close session: the cash must be handed over and accepted before closing.");

        // DEF-E2E-08/11: back each payment down to its fee/full-bill "base" (CollectedAmount minus
        // every individually collected item for that registration, wherever collected), then add
        // items by the session they were actually collected in instead of the payment's own.
        var payments = await opdPaymentRepository.GetBySessionIdAsync(request.Id, request.ApplicationId, cancellationToken);
        var itemTotals = await opdParticularRepository.GetIndividuallyCollectedTotalsByRegistrationIdsAsync(
            payments.Select(p => p.OpdRegistrationId), request.ApplicationId, cancellationToken);
        var baseCollected = payments.Sum(p => p.CollectedAmount - itemTotals.GetValueOrDefault(p.OpdRegistrationId));
        var itemsCollectedHere = (await opdParticularRepository.GetCollectedBySessionIdAsync(request.Id, request.ApplicationId, cancellationToken)).Sum(i => i.Amount);
        var totalCollected = baseCollected + itemsCollectedHere;

        var expenses = await expenseRecordRepository.GetBySessionIdAsync(request.Id, request.ApplicationId, cancellationToken);
        var totalExpenses = expenses.Sum(e => e.PaidAmount);

        session.Close(totalCollected, totalExpenses);
        counterSessionRepository.Update(session);
        await counterSessionRepository.SaveChangesAsync(cancellationToken);
        return CounterSessionTranslator.ToDto(session);
    }
}
