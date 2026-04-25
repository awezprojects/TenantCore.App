using MediatR;
using TenantCore.Application.Features.OpdRegistrations.Commands;
using TenantCore.Application.Features.OpdRegistrations.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdRegistrations.Handlers;

public sealed class UpdateOpdRegistrationHandler(IOpdRegistrationRepository repository)
    : IRequestHandler<UpdateOpdRegistrationCommand, OpdRegistrationDto>
{
    public async Task<OpdRegistrationDto> Handle(
        UpdateOpdRegistrationCommand request, CancellationToken cancellationToken)
    {
        var registration = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(OpdRegistration), request.Id);

        if (registration.ApplicationId != request.ApplicationId)
            throw new UnauthorizedAccessException("Access denied.");

        registration.Update(request.DoctorUserId, request.DoctorName, request.Fee, request.Status, request.Notes);
        repository.Update(registration);
        await repository.SaveChangesAsync(cancellationToken);

        return OpdRegistrationTranslator.ToDto(registration);
    }
}
