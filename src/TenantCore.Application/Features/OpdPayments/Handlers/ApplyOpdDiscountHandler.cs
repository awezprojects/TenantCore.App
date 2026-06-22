using MediatR;
using TenantCore.Application.Features.OpdPayments.Commands;
using TenantCore.Application.Features.OpdPayments.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdPayments.Handlers;

public sealed class ApplyOpdDiscountHandler(IOpdPaymentRepository repository) : IRequestHandler<ApplyOpdDiscountCommand, OpdPaymentDto>
{
    public async Task<OpdPaymentDto> Handle(ApplyOpdDiscountCommand request, CancellationToken cancellationToken)
    {
        var payment = await repository.GetByOpdRegistrationIdAsync(request.Request.OpdRegistrationId, request.ApplicationId, cancellationToken);
        if (payment is null)
            throw new NotFoundException(nameof(OpdPayment), request.Request.OpdRegistrationId);

        if (request.Request.Discount > payment.TotalAmount)
            throw new InvalidOperationException("Discount cannot exceed the total amount.");

        payment.ApplyDiscount(request.Request.Discount);
        repository.Update(payment);
        await repository.SaveChangesAsync(cancellationToken);
        return OpdPaymentTranslator.ToDto(payment);
    }
}
