using MediatR;
using TenantCore.Application.Features.OpdPayments.Commands;
using TenantCore.Application.Features.OpdPayments.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdPayments.Handlers;

public sealed class ProcessOpdRefundHandler(IOpdPaymentRepository repository)
    : IRequestHandler<ProcessOpdRefundCommand, OpdPaymentDto>
{
    // DEF-OPD-04: this used to also reverse the OPD usage charge on every refund — including a
    // partial discount on a visit that still goes ahead. The usage charge pays for registering the
    // visit, not for the money collected from the patient, so it is reversed exactly once, when the
    // visit is actually cancelled (UpdateOpdRegistrationHandler), never here.
    public async Task<OpdPaymentDto> Handle(ProcessOpdRefundCommand request, CancellationToken cancellationToken)
    {
        var payment = await repository.GetByOpdRegistrationIdAsync(
            request.Request.OpdRegistrationId, request.ApplicationId, cancellationToken);
        if (payment is null)
            throw new NotFoundException(nameof(OpdPayment), request.Request.OpdRegistrationId);

        // DEF-OPD-16: the route's {id} was never checked against the body, so a mismatched payment
        // id in the path silently refunded whatever OpdRegistrationId the body carried.
        if (payment.Id != request.PaymentId)
            throw new NotFoundException(nameof(OpdPayment), request.PaymentId);

        payment.ProcessRefund(request.RefundedByUserId);
        repository.Update(payment);

        await repository.SaveChangesAsync(cancellationToken);
        return OpdPaymentTranslator.ToDto(payment);
    }
}
