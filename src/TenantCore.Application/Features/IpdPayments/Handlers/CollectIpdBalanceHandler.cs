using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdPayments.Commands;
using TenantCore.Application.Features.IpdPayments.Services;
using TenantCore.Application.Features.IpdPayments.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdPayments.Handlers;

public sealed class CollectIpdBalanceHandler(
    IIpdRegistrationRepository ipdRepository,
    IIpdPaymentRepository paymentRepository,
    ICounterSessionRepository counterSessionRepository,
    IIpdBillCalculator billCalculator,
    IApplicationAccessValidator accessValidator)
    : IRequestHandler<CollectIpdBalanceCommand, IpdFinalBillDto>
{
    public async Task<IpdFinalBillDto> Handle(CollectIpdBalanceCommand request, CancellationToken cancellationToken)
    {
        var payment = await paymentRepository.GetByRegistrationAsync(request.IpdRegistrationId, request.ApplicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(IpdPayment), request.IpdRegistrationId);

        if (!accessValidator.CanAccess(payment.ApplicationId))
            throw new UnauthorizedAccessException("Access denied.");

        var activeSession = await counterSessionRepository.GetActiveSessionAsync(request.ApplicationId, cancellationToken);

        payment.CollectBalance(request.ReceivedByUserId, activeSession?.Id);
        paymentRepository.Update(payment);
        await paymentRepository.SaveChangesAsync(cancellationToken);

        // The bill is already finalized by now (CollectBalance requires FinalizedAt) — recompute
        // the room-charge line items for display using the same as-of timestamp it was locked at.
        // Occupancy history is immutable after discharge, so this is deterministic and matches
        // the persisted RoomChargesTotal exactly.
        var registration = await ipdRepository.GetByIdAsync(request.IpdRegistrationId, cancellationToken)
            ?? throw new NotFoundException(nameof(IpdRegistration), request.IpdRegistrationId);
        var roomCharges = await billCalculator.CalculateRoomChargesAsync(registration, payment.FinalizedAt!.Value, cancellationToken);
        return IpdPaymentTranslator.ToFinalBillDto(payment, roomCharges);
    }
}
