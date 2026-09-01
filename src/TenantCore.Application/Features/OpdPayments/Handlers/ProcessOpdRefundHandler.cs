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
    public async Task<OpdPaymentDto> Handle(ProcessOpdRefundCommand request, CancellationToken cancellationToken)
    {
        var payment = await repository.GetByOpdRegistrationIdAsync(
            request.Request.OpdRegistrationId, request.ApplicationId, cancellationToken);
        if (payment is null)
            throw new NotFoundException(nameof(OpdPayment), request.Request.OpdRegistrationId);

        payment.ProcessRefund(request.RefundedByUserId);
        repository.Update(payment);
        await repository.SaveChangesAsync(cancellationToken);
        return OpdPaymentTranslator.ToDto(payment);
    }
}
