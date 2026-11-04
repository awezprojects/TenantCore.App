using MediatR;
using TenantCore.Application.Features.OpdRegistrations.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.OpdRegistrations.Handlers;

public sealed class DeleteOpdRegistrationHandler(
    IOpdRegistrationRepository opdRepository,
    IOpdPaymentRepository paymentRepository,
    IOpdParticularRepository particularRepository) : IRequestHandler<DeleteOpdRegistrationCommand>
{
    public async Task Handle(DeleteOpdRegistrationCommand request, CancellationToken cancellationToken)
    {
        var registration = await opdRepository.GetByIdAsync(request.Id, cancellationToken);
        if (registration is null || registration.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(OpdRegistration), request.Id);

        if (registration.Status != OpdStatus.Cancelled)
            throw new InvalidOperationException("Only a cancelled OPD registration can be deleted.");

        // DEF-OPD-09: a ₹0 visit is marked Received (AcceptVisitFee treats "received ₹0" as
        // collected) but there is nothing to hand back — CollectedAmount, not PaymentStatus
        // alone, decides whether a refund is actually owed before delete is allowed.
        var payment = await paymentRepository.GetByOpdRegistrationIdAsync(request.Id, request.ApplicationId, cancellationToken);
        if (payment is not null && payment.PaymentStatus == PaymentStatus.Received
            && payment.RefundStatus != RefundStatus.Refunded && payment.CollectedAmount > 0)
            throw new InvalidOperationException("Refund the collected amount before deleting this OPD registration.");

        var particulars = await particularRepository.GetByOpdRegistrationIdAsync(request.Id, request.ApplicationId, cancellationToken);
        var particularsList = particulars.ToList();
        if (particularsList.Any(p => p.PaymentStatus == PaymentStatus.Received))
            throw new InvalidOperationException("Refund the collected amount for individually collected service items before deleting this OPD registration.");

        foreach (var particular in particularsList)
            particularRepository.Delete(particular);

        if (payment is not null)
            paymentRepository.Delete(payment);

        opdRepository.Delete(registration);

        await opdRepository.SaveChangesAsync(cancellationToken);
    }
}
