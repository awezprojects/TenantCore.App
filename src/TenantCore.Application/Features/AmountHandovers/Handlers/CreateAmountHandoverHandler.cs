using MediatR;
using TenantCore.Application.Features.AmountHandovers.Commands;
using TenantCore.Application.Features.AmountHandovers.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.AmountHandovers.Handlers;

public sealed class CreateAmountHandoverHandler(
    IAmountHandoverRepository handoverRepository,
    ICounterSessionRepository sessionRepository,
    IOpdPaymentRepository opdPaymentRepository,
    IOpdParticularRepository opdParticularRepository,
    IExpenseRecordRepository expenseRecordRepository) : IRequestHandler<CreateAmountHandoverCommand, Guid>
{
    public async Task<Guid> Handle(CreateAmountHandoverCommand request, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(request.Request.CounterSessionId, cancellationToken);
        if (session is null || session.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(CounterSession), request.Request.CounterSessionId);

        // DEF-MR-02: a receptionist sending only part of the session's cash (e.g. "my own share"
        // in a multi-reception clinic) used to be accepted here with a 201 and only fail later,
        // at Accept, with a 409 the sender never sees. Reject a mismatched amount up front with a
        // message the sender can act on immediately. Same balance formula as
        // AcceptAmountHandoverHandler.EnsureHandoverMatchesSessionBalanceAsync.
        if (session.Status == CounterSessionStatus.Open)
        {
            var existingHandovers = (await handoverRepository.GetBySessionIdAsync(session.Id, request.ApplicationId, cancellationToken)).ToList();

            // DEF-MR-02 (QA re-test): a second handover could be raised for this session while an
            // earlier one was still Pending, letting two handovers compete for the same cash.
            if (existingHandovers.Any(h => h.Status == HandoverStatus.Pending))
                throw new InvalidOperationException(
                    "A handover for this session is already pending. Wait for it to be accepted or disputed first.");

            var netAmount = await ComputeSessionNetAmountAsync(session.Id, request.ApplicationId, cancellationToken);

            // DEF-MR-02 (QA re-test): netAmount is the session's cumulative collected total and
            // never shrinks once a handover is accepted. More cash collected into an open session
            // after an earlier handover was accepted was letting a new handover be raised against
            // the FULL cumulative total again, double-counting the cash already handed over. Only
            // what's still outstanding — collected minus already accepted for this session — can
            // be handed over now.
            var alreadyAccepted = existingHandovers.Where(h => h.Status == HandoverStatus.Accepted).Sum(h => h.Amount);
            var outstanding = netAmount - alreadyAccepted;

            if (request.Request.Amount != outstanding)
                throw new InvalidOperationException(
                    $"Handover amount Rs. {request.Request.Amount:N2} does not match the counter session's " +
                    $"outstanding balance (Rs. {netAmount:N2} collected minus Rs. {alreadyAccepted:N2} already " +
                    $"handed over and accepted) of Rs. {outstanding:N2}. A handover must cover the whole " +
                    "outstanding balance — per-receptionist partial handovers are not supported.");
        }

        var handover = AmountHandoverTranslator.ToEntity(request);
        await handoverRepository.AddAsync(handover, cancellationToken);
        await handoverRepository.SaveChangesAsync(cancellationToken);
        return handover.Id;
    }

    private async Task<decimal> ComputeSessionNetAmountAsync(Guid sessionId, Guid applicationId, CancellationToken cancellationToken)
    {
        var payments = await opdPaymentRepository.GetBySessionIdAsync(sessionId, applicationId, cancellationToken);
        var expenses = await expenseRecordRepository.GetBySessionIdAsync(sessionId, applicationId, cancellationToken);
        var itemTotals = await opdParticularRepository.GetIndividuallyCollectedTotalsByRegistrationIdsAsync(
            payments.Select(p => p.OpdRegistrationId), applicationId, cancellationToken);
        var baseCollected = payments.Sum(p => p.CollectedAmount - itemTotals.GetValueOrDefault(p.OpdRegistrationId));
        var itemsCollectedHere = (await opdParticularRepository.GetCollectedBySessionIdAsync(sessionId, applicationId, cancellationToken)).Sum(i => i.Amount);
        return baseCollected + itemsCollectedHere - expenses.Sum(e => e.PaidAmount);
    }
}
