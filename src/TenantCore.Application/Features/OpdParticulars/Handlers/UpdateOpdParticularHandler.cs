using MediatR;
using TenantCore.Application.Features.OpdParticulars.Commands;
using TenantCore.Application.Features.OpdParticulars.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdParticulars.Handlers;

public sealed class UpdateOpdParticularHandler(
    IOpdParticularRepository opdParticularRepository,
    IOpdPaymentRepository opdPaymentRepository) : IRequestHandler<UpdateOpdParticularCommand, OpdParticularDto>
{
    public async Task<OpdParticularDto> Handle(UpdateOpdParticularCommand request, CancellationToken cancellationToken)
    {
        var opdParticular = await opdParticularRepository.GetByIdAsync(request.Id, cancellationToken);
        if (opdParticular is null || opdParticular.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(OpdParticular), request.Id);

        opdParticular.Update(request.Request.Amount);
        opdParticularRepository.Update(opdParticular);
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

        return OpdParticularTranslator.ToDto(opdParticular);
    }
}
