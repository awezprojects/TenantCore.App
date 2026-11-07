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
        var expenses = await expenseRecordRepository.GetBySessionIdAsync(session.Id, applicationId, cancellationToken);

        // DEF-E2E-08/11: back each payment down to its fee/full-bill "base" (CollectedAmount minus
        // every individually collected item for that registration, wherever collected), then add
        // items by the session they were actually collected in instead of the payment's own.
        var itemTotals = await opdParticularRepository.GetIndividuallyCollectedTotalsByRegistrationIdsAsync(
            payments.Select(p => p.OpdRegistrationId), applicationId, cancellationToken);
        var baseCollected = payments.Sum(p => p.CollectedAmount - itemTotals.GetValueOrDefault(p.OpdRegistrationId));
        var itemsCollectedHere = (await opdParticularRepository.GetCollectedBySessionIdAsync(session.Id, applicationId, cancellationToken)).Sum(i => i.Amount);
        var netAmount = baseCollected + itemsCollectedHere - expenses.Sum(e => e.PaidAmount);

        // DEF-MR-02 (QA re-test): netAmount above is the session's cumulative collected total —
        // it never shrinks when a handover is accepted. A session that stays open after an
        // accepted handover (e.g. more cash collected afterwards) was letting a second handover
        // be raised and accepted against the FULL cumulative total again, double-counting the
        // cash already handed over. Only the amount still outstanding — net collected minus what
        // was already accepted for this same session — is available to hand over.
        var alreadyAccepted = (await repository.GetBySessionIdAsync(session.Id, applicationId, cancellationToken))
            .Where(h => h.Id != handover.Id && h.Status == HandoverStatus.Accepted)
            .Sum(h => h.Amount);
        var outstanding = netAmount - alreadyAccepted;

        if (handover.Amount != outstanding)
            throw new InvalidOperationException(
                $"This handover is for Rs. {handover.Amount:N2} but the counter session's outstanding balance " +
                $"(Rs. {netAmount:N2} collected minus Rs. {alreadyAccepted:N2} already handed over and accepted) " +
                $"is now Rs. {outstanding:N2}. Dispute it so Reception can send the corrected amount.");
    }
}
