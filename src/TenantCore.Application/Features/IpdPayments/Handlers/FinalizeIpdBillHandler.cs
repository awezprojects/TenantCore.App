using MediatR;
using TenantCore.Application.Features.IpdPayments.Commands;
using TenantCore.Application.Features.IpdPayments.Services;
using TenantCore.Application.Features.IpdPayments.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdPayments.Handlers;

// Dispatched as an internal sub-command, before the parent (DischargePatientHandler)'s own
// SaveChangesAsync — same pattern as ChargeUsageCommand. Deliberately does NOT call its own
// SaveChangesAsync: the discharge, the bed release and the finalized bill must commit as one
// transaction, which only the outer handler's single save can guarantee.
public sealed class FinalizeIpdBillHandler(
    IIpdRegistrationRepository ipdRepository,
    IIpdPaymentRepository paymentRepository,
    IIpdBillCalculator billCalculator)
    : IRequestHandler<FinalizeIpdBillCommand, IpdFinalBillDto?>
{
    public async Task<IpdFinalBillDto?> Handle(FinalizeIpdBillCommand request, CancellationToken cancellationToken)
    {
        var payment = await paymentRepository.GetByRegistrationAsync(request.IpdRegistrationId, request.ApplicationId, cancellationToken);
        if (payment is null)
            return null; // Billing disabled for this clinic — nothing to finalize.

        var registration = await ipdRepository.GetByIdAsync(request.IpdRegistrationId, cancellationToken)
            ?? throw new NotFoundException(nameof(IpdRegistration), request.IpdRegistrationId);

        var roomCharges = await billCalculator.CalculateRoomChargesAsync(registration, request.DischargeDate, cancellationToken);

        payment.FinalizeAtDischarge(roomCharges.RoomChargesTotal, request.DischargeDate);
        paymentRepository.Update(payment);

        return IpdPaymentTranslator.ToFinalBillDto(payment, roomCharges);
    }
}
