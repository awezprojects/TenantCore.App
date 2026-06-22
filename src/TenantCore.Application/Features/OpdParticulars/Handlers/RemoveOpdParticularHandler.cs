using MediatR;
using TenantCore.Application.Features.OpdParticulars.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.OpdParticulars.Handlers;

public sealed class RemoveOpdParticularHandler(
    IOpdParticularRepository opdParticularRepository,
    IOpdPaymentRepository opdPaymentRepository) : IRequestHandler<RemoveOpdParticularCommand>
{
    public async Task Handle(RemoveOpdParticularCommand request, CancellationToken cancellationToken)
    {
        var opdParticular = await opdParticularRepository.GetByIdAsync(request.Id, cancellationToken);
        if (opdParticular is null || opdParticular.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(OpdParticular), request.Id);

        if (opdParticular.PaymentStatus == PaymentStatus.Received)
            throw new InvalidOperationException(
                $"Cannot remove '{opdParticular.ParticularName}' — it has already been collected.");

        opdParticularRepository.Delete(opdParticular);
        await opdParticularRepository.SaveChangesAsync(cancellationToken);

        var newTotal = await opdParticularRepository.GetTotalByOpdRegistrationIdAsync(
            opdParticular.OpdRegistrationId, request.ApplicationId, cancellationToken);

        var payment = await opdPaymentRepository.GetByOpdRegistrationIdAsync(
            opdParticular.OpdRegistrationId, request.ApplicationId, cancellationToken);
        if (payment is not null)
        {
            payment.UpdateParticularsTotal(newTotal);
            opdPaymentRepository.Update(payment);
            await opdPaymentRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
