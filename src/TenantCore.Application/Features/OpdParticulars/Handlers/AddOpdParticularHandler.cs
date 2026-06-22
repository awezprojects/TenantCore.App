using MediatR;
using TenantCore.Application.Features.OpdParticulars.Commands;
using TenantCore.Application.Features.OpdParticulars.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdParticulars.Handlers;

public sealed class AddOpdParticularHandler(
    IOpdParticularRepository opdParticularRepository,
    IOpdPaymentRepository opdPaymentRepository,
    IParticularRepository particularRepository) : IRequestHandler<AddOpdParticularCommand, OpdParticularDto>
{
    public async Task<OpdParticularDto> Handle(AddOpdParticularCommand request, CancellationToken cancellationToken)
    {
        var particular = await particularRepository.GetByIdAsync(request.Request.ParticularId, cancellationToken);
        if (particular is null || particular.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(Particular), request.Request.ParticularId);

        var opdParticular = OpdParticularTranslator.ToEntity(request, particular.Name);
        await opdParticularRepository.AddAsync(opdParticular, cancellationToken);

        // Keep OpdPayment.ParticularsTotal in sync. Compute in-memory (stored total + new item)
        // so the entire operation is committed atomically in one SaveChangesAsync.
        var payment = await opdPaymentRepository.GetByOpdRegistrationIdAsync(
            request.Request.OpdRegistrationId, request.ApplicationId, cancellationToken);
        if (payment is not null)
        {
            payment.UpdateParticularsTotal(payment.ParticularsTotal + opdParticular.Amount);
            opdPaymentRepository.Update(payment);
        }

        await opdParticularRepository.SaveChangesAsync(cancellationToken);
        return OpdParticularTranslator.ToDto(opdParticular);
    }
}
