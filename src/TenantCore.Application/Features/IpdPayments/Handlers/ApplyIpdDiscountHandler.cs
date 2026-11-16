using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdPayments.Commands;
using TenantCore.Application.Features.IpdPayments.Services;
using TenantCore.Application.Features.IpdPayments.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Errors;

namespace TenantCore.Application.Features.IpdPayments.Handlers;

// RequireClinicStaff at the controller — reception or doctor, same policy OPD's discount uses.
public sealed class ApplyIpdDiscountHandler(
    IIpdRegistrationRepository ipdRepository,
    IIpdPaymentRepository paymentRepository,
    IIpdBillCalculator billCalculator,
    IApplicationAccessValidator accessValidator)
    : IRequestHandler<ApplyIpdDiscountCommand, IpdProvisionalBillDto>
{
    public async Task<IpdProvisionalBillDto> Handle(ApplyIpdDiscountCommand request, CancellationToken cancellationToken)
    {
        var registration = await ipdRepository.GetByIdAsync(request.IpdRegistrationId, cancellationToken)
            ?? throw new NotFoundException(nameof(IpdRegistration), request.IpdRegistrationId);

        if (!accessValidator.CanAccess(registration.ApplicationId))
            throw new UnauthorizedAccessException("Access denied.");

        var payment = await paymentRepository.GetByRegistrationAsync(registration.Id, request.ApplicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(IpdPayment), request.IpdRegistrationId);

        var asOfUtc = DateTime.UtcNow;
        var roomCharges = await billCalculator.CalculateRoomChargesAsync(registration, asOfUtc, cancellationToken);

        if (request.Discount > payment.InitialFee + payment.ParticularsTotal + roomCharges.RoomChargesTotal)
            throw new InvalidOperationException("Discount cannot exceed the total bill amount.");

        payment.ApplyDiscount(request.Discount, request.DiscountedByUserId);
        paymentRepository.Update(payment);
        await paymentRepository.SaveChangesAsync(cancellationToken);

        return IpdPaymentTranslator.ToProvisionalBillDto(registration, payment, roomCharges, asOfUtc);
    }
}
