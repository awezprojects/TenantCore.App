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

public sealed class ProcessIpdRefundHandler(
    IIpdRegistrationRepository ipdRepository,
    IIpdPaymentRepository paymentRepository,
    IIpdBillCalculator billCalculator,
    IApplicationAccessValidator accessValidator)
    : IRequestHandler<ProcessIpdRefundCommand, IpdFinalBillDto>
{
    public async Task<IpdFinalBillDto> Handle(ProcessIpdRefundCommand request, CancellationToken cancellationToken)
    {
        var payment = await paymentRepository.GetByRegistrationAsync(request.IpdRegistrationId, request.ApplicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(IpdPayment), request.IpdRegistrationId);

        if (!accessValidator.CanAccess(payment.ApplicationId))
            throw new UnauthorizedAccessException("Access denied.");

        payment.ProcessRefund(request.RefundedByUserId);
        paymentRepository.Update(payment);
        await paymentRepository.SaveChangesAsync(cancellationToken);

        var registration = await ipdRepository.GetByIdAsync(request.IpdRegistrationId, cancellationToken)
            ?? throw new NotFoundException(nameof(IpdRegistration), request.IpdRegistrationId);
        var roomCharges = await billCalculator.CalculateRoomChargesAsync(registration, payment.FinalizedAt!.Value, cancellationToken);
        return IpdPaymentTranslator.ToFinalBillDto(payment, roomCharges);
    }
}
