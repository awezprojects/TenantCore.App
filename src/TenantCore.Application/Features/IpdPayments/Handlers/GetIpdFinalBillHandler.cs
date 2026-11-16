using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdPayments.Queries;
using TenantCore.Application.Features.IpdPayments.Services;
using TenantCore.Application.Features.IpdPayments.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;
using TenantCore.Shared.Errors;

namespace TenantCore.Application.Features.IpdPayments.Handlers;

// Only returns a result once FinalizeIpdBillCommand has run (i.e. after discharge) — the bill
// is locked, not the live view GetIpdProvisionalBillHandler serves while still admitted.
public sealed class GetIpdFinalBillHandler(
    IIpdRegistrationRepository ipdRepository,
    IIpdPaymentRepository paymentRepository,
    IIpdBillCalculator billCalculator,
    IApplicationAccessValidator accessValidator)
    : IRequestHandler<GetIpdFinalBillQuery, IpdFinalBillDto>
{
    public async Task<IpdFinalBillDto> Handle(GetIpdFinalBillQuery request, CancellationToken cancellationToken)
    {
        if (!accessValidator.CanAccess(request.ApplicationId))
            throw new UnauthorizedAccessException("Access denied.");

        var payment = await paymentRepository.GetByRegistrationAsync(request.IpdRegistrationId, request.ApplicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(IpdPayment), request.IpdRegistrationId);

        if (payment.FinalizedAt is null)
            throw new InvalidOperationException(UserMessages.IpdBillNotFinalized);

        var registration = await ipdRepository.GetByIdAsync(request.IpdRegistrationId, cancellationToken)
            ?? throw new NotFoundException(nameof(IpdRegistration), request.IpdRegistrationId);

        var roomCharges = await billCalculator.CalculateRoomChargesAsync(registration, payment.FinalizedAt.Value, cancellationToken);
        // DEF-IPD-22: tells the UI/print page this is a cancelled admission's forced-₹0 bill, not
        // a real discharge's locked one — without it, the figures above (real, un-waived
        // InitialFee/RoomChargesTotal/line items) next to a ₹0.00 Total looked like they didn't add up.
        return IpdPaymentTranslator.ToFinalBillDto(payment, roomCharges, registration.Status == IpdStatus.Cancelled);
    }
}
