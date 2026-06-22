using MediatR;
using TenantCore.Application.Features.OpdPayments.Commands;
using TenantCore.Application.Features.OpdPayments.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.OpdPayments.Handlers;

public sealed class AcceptOpdPaymentHandler(IOpdPaymentRepository repository) : IRequestHandler<AcceptOpdPaymentCommand, OpdPaymentDto>
{
    public async Task<OpdPaymentDto> Handle(AcceptOpdPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await repository.GetByOpdRegistrationIdAsync(request.Request.OpdRegistrationId, request.ApplicationId, cancellationToken);
        if (payment is null)
            throw new NotFoundException(nameof(OpdPayment), request.Request.OpdRegistrationId);

        if (payment.PaymentStatus == PaymentStatus.Received)
            throw new InvalidOperationException("Payment has already been received.");

        payment.Accept(request.ReceivedByUserId, request.Request.CounterSessionId);
        repository.Update(payment);
        await repository.SaveChangesAsync(cancellationToken);
        return OpdPaymentTranslator.ToDto(payment);
    }
}
