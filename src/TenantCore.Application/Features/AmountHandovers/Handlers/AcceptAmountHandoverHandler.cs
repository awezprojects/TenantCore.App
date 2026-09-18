using MediatR;
using TenantCore.Application.Features.AmountHandovers.Commands;
using TenantCore.Application.Features.AmountHandovers.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.AmountHandovers.Handlers;

public sealed class AcceptAmountHandoverHandler(
    IAmountHandoverRepository repository,
    ICounterSessionRepository counterSessionRepository,
    IOpdPaymentRepository opdPaymentRepository,
    IOpdParticularRepository opdParticularRepository,
    IExpenseRecordRepository expenseRecordRepository)
    : IRequestHandler<AcceptAmountHandoverCommand, AmountHandoverDto>
{
    public async Task<AmountHandoverDto> Handle(AcceptAmountHandoverCommand request, CancellationToken cancellationToken)
    {
        var handover = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (handover is null || handover.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(AmountHandover), request.Id);

        if (handover.Status != HandoverStatus.Pending)
            throw new InvalidOperationException("Only pending handovers can be accepted.");

        await EnsureHandoverMatchesSessionBalanceAsync(handover, request.ApplicationId, cancellationToken);

        handover.Accept();
        repository.Update(handover);
        await repository.SaveChangesAsync(cancellationToken);
        return AmountHandoverTranslator.ToDto(handover);
    }

    // The counter's balance can change between a handover being sent and accepted — most
    // commonly a refund processed in the meantime, which reduces the session's collected total
    // while the pending handover keeps its original amount. Accepting it would have the
    // recipient sign for cash the till no longer holds, so refuse and let Reception dispute
    // and resend the corrected amount.
    private async Task EnsureHandoverMatchesSessionBalanceAsync(
        AmountHandover handover, Guid applicationId, CancellationToken cancellationToken)
    {
        var session = await counterSessionRepository.GetByIdAsync(handover.CounterSessionId, cancellationToken);
        if (session is null || session.ApplicationId != applicationId
            || session.Status != CounterSessionStatus.Open)
            return;

        var payments = await opdPaymentRepository.GetBySessionIdAsync(session.Id, applicationId, cancellationToken);
        var particulars = await opdParticularRepository.GetCollectedBySessionIdAsync(session.Id, applicationId, cancellationToken);
        var expenses = await expenseRecordRepository.GetBySessionIdAsync(session.Id, applicationId, cancellationToken);

        var netAmount = payments.Sum(p => p.CollectedAmount)
                      + particulars.Sum(p => p.Amount)
                      - expenses.Sum(e => e.PaidAmount);

        if (handover.Amount != netAmount)
            throw new InvalidOperationException(
                $"This handover is for Rs. {handover.Amount:N2} but the counter session balance is now " +
                $"Rs. {netAmount:N2}. Dispute it so Reception can send the corrected amount.");
    }
}
