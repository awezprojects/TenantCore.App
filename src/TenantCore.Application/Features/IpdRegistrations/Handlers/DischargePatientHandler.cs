using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdRegistrations.Commands;
using TenantCore.Application.Features.IpdRegistrations.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdRegistrations.Handlers;

public sealed class DischargePatientHandler(
    IIpdRegistrationRepository repository,
    IBedRepository bedRepository,
    IApplicationAccessValidator accessValidator)
    : IRequestHandler<DischargePatientCommand, IpdRegistrationDto>
{
    public async Task<IpdRegistrationDto> Handle(
        DischargePatientCommand request, CancellationToken cancellationToken)
    {
        var registration = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(IpdRegistration), request.Id);

        if (!accessValidator.CanAccess(registration.ApplicationId))
            throw new UnauthorizedAccessException("Access denied.");

        // Free the bed before discharging
        if (!string.IsNullOrWhiteSpace(registration.WardName) &&
            !string.IsNullOrWhiteSpace(registration.RoomNumber) &&
            !string.IsNullOrWhiteSpace(registration.BedNumber))
        {
            var bed = await bedRepository.FindByLocationAsync(
                registration.ApplicationId, registration.WardName, registration.RoomNumber, registration.BedNumber, cancellationToken);
            bed?.MarkAvailable();
        }

        registration.Discharge(request.DischargeNotes);
        repository.Update(registration);
        await repository.SaveChangesAsync(cancellationToken);

        return IpdRegistrationTranslator.ToDto(registration);
    }
}
