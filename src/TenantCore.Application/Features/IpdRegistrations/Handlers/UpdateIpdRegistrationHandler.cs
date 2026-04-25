using MediatR;
using TenantCore.Application.Features.IpdRegistrations.Commands;
using TenantCore.Application.Features.IpdRegistrations.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdRegistrations.Handlers;

public sealed class UpdateIpdRegistrationHandler(IIpdRegistrationRepository repository)
    : IRequestHandler<UpdateIpdRegistrationCommand, IpdRegistrationDto>
{
    public async Task<IpdRegistrationDto> Handle(
        UpdateIpdRegistrationCommand request, CancellationToken cancellationToken)
    {
        var registration = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(IpdRegistration), request.Id);

        if (registration.ApplicationId != request.ApplicationId)
            throw new UnauthorizedAccessException("Access denied.");

        registration.Update(
            request.DoctorUserId, request.DoctorName,
            request.WardName, request.RoomNumber, request.BedNumber,
            request.Status, request.AdmissionNotes);

        repository.Update(registration);
        await repository.SaveChangesAsync(cancellationToken);

        return IpdRegistrationTranslator.ToDto(registration);
    }
}
