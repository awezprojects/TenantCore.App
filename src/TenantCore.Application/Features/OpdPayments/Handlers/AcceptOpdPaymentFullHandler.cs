using MediatR;
using TenantCore.Application.Features.OpdPayments.Commands;
using TenantCore.Application.Features.OpdPayments.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.OpdPayments.Handlers;

public sealed class AcceptOpdPaymentFullHandler(
    IOpdPaymentRepository paymentRepository,
    IOpdParticularRepository particularRepository) : IRequestHandler<AcceptOpdPaymentFullCommand, OpdPaymentDto>
{
    public async Task<OpdPaymentDto> Handle(AcceptOpdPaymentFullCommand request, CancellationToken cancellationToken)
    {
        var payment = await paymentRepository.GetByOpdRegistrationIdAsync(
            request.Request.OpdRegistrationId, request.ApplicationId, cancellationToken);
        if (payment is null)
            throw new NotFoundException(nameof(OpdPayment), request.Request.OpdRegistrationId);

        if (payment.Discount <= 0)
            throw new InvalidOperationException(
                "Use 'Collect Visit Fee' for no-discount payments. This endpoint is for discounted full-bill collection only.");

        if (payment.PaymentStatus == PaymentStatus.Received)
            throw new InvalidOperationException("Payment has already been fully collected.");

        // Collect the entire bill (VisitFee + ParticularsTotal - Discount) as one transaction.
        payment.AcceptFull(request.ReceivedByUserId, request.Request.CounterSessionId);
        paymentRepository.Update(payment);

        // Mark all service items as received. CounterSessionId = null because the payment-level
        // CollectedAmount (= FinalAmount) already covers them — no individual counter credit.
        var items = await particularRepository.GetByOpdRegistrationIdAsync(
            request.Request.OpdRegistrationId, request.ApplicationId, cancellationToken);
        foreach (var item in items.Where(i => i.PaymentStatus == PaymentStatus.Pending))
        {
            item.MarkCollectedViaPayment(request.ReceivedByUserId);
            particularRepository.Update(item);
        }

        await paymentRepository.SaveChangesAsync(cancellationToken);
        return OpdPaymentTranslator.ToDto(payment);
    }
}
