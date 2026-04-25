using MediatR;
using TenantCore.Application.Features.IpdRegistrations.Commands;
using TenantCore.Application.Features.IpdRegistrations.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdRegistrations.Handlers;

public sealed class DischargePatientHandler(IIpdRegistrationRepository repository)
    : IRequestHandler<DischargePatientCommand, IpdRegistrationDto>
{
    public async Task<IpdRegistrationDto> Handle(
        DischargePatientCommand request, CancellationToken cancellationToken)
    {
        var registration = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(IpdRegistration), request.Id);

        if (registration.ApplicationId != request.ApplicationId)
            throw new UnauthorizedAccessException("Access denied.");

        registration.Discharge(request.DischargeNotes);
        repository.Update(registration);
        await repository.SaveChangesAsync(cancellationToken);

        return IpdRegistrationTranslator.ToDto(registration);
    }
}
