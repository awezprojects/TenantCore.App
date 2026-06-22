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

        // Total = OpdPayment.CollectedAmount (visit fee or discounted full-bill)
        //       + OpdParticular.Amount (individually collected service items)
        var payments = await opdPaymentRepository.GetBySessionIdAsync(request.Id, request.ApplicationId, cancellationToken);
        var particulars = await opdParticularRepository.GetCollectedBySessionIdAsync(request.Id, request.ApplicationId, cancellationToken);
        var totalCollected = payments.Sum(p => p.CollectedAmount) + particulars.Sum(p => p.Amount);

        var expenses = await expenseRecordRepository.GetBySessionIdAsync(request.Id, request.ApplicationId, cancellationToken);
        var totalExpenses = expenses.Sum(e => e.PaidAmount);

        session.Close(totalCollected, totalExpenses);
        counterSessionRepository.Update(session);
        await counterSessionRepository.SaveChangesAsync(cancellationToken);
        return CounterSessionTranslator.ToDto(session);
    }
}
