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
            var netAmount = await ComputeSessionNetAmountAsync(session.Id, request.ApplicationId, cancellationToken);
            if (request.Request.Amount != netAmount)
                throw new InvalidOperationException(
                    $"Handover amount Rs. {request.Request.Amount:N2} does not match the counter session's " +
                    $"current balance of Rs. {netAmount:N2}. A handover must cover the whole session balance — " +
                    "per-receptionist partial handovers are not supported.");
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
