using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdRegistrations.Queries;
using TenantCore.Application.Features.IpdRegistrations.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdRegistrations.Handlers;

public sealed class GetIpdRegistrationByIdHandler(IIpdRegistrationRepository repository, IApplicationAccessValidator accessValidator)
    : IRequestHandler<GetIpdRegistrationByIdQuery, IpdRegistrationDto>
{
    public async Task<IpdRegistrationDto> Handle(
        GetIpdRegistrationByIdQuery request, CancellationToken cancellationToken)
    {
        var registration = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(IpdRegistration), request.Id);

        if (!accessValidator.CanAccess(registration.ApplicationId))
            throw new UnauthorizedAccessException("Access denied.");

        return IpdRegistrationTranslator.ToDto(registration);
    }
}
