using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.OpdRegistrations.Queries;
using TenantCore.Application.Features.OpdRegistrations.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdRegistrations.Handlers;

public sealed class GetOpdRegistrationByIdHandler(
    IOpdRegistrationRepository repository,
    IOpdPaymentRepository paymentRepository,
    IApplicationAccessValidator accessValidator)
    : IRequestHandler<GetOpdRegistrationByIdQuery, OpdRegistrationDto>
{
    public async Task<OpdRegistrationDto> Handle(
        GetOpdRegistrationByIdQuery request, CancellationToken cancellationToken)
    {
        var registration = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(OpdRegistration), request.Id);

        if (!accessValidator.CanAccess(registration.ApplicationId))
            throw new UnauthorizedAccessException("Access denied.");

        var payment = await paymentRepository.GetByOpdRegistrationIdAsync(request.Id, registration.ApplicationId, cancellationToken);
        return OpdRegistrationTranslator.ToDto(registration, payment);
    }
}
