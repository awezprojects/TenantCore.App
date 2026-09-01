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

        var payment = await paymentRepository.GetByOpdRegistrationIdAsync(request.Id, request.ApplicationId, cancellationToken);
        if (payment is not null && payment.PaymentStatus == PaymentStatus.Received && payment.RefundStatus != RefundStatus.Refunded)
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
