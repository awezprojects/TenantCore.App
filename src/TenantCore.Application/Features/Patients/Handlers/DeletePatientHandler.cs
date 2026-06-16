using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.Patients.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.Patients.Handlers;

public sealed class DeletePatientHandler(IPatientRepository repository, IApplicationAccessValidator accessValidator)
    : IRequestHandler<DeletePatientCommand>
{
    public async Task Handle(DeletePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Patient), request.Id);

        if (!accessValidator.CanAccess(patient.ApplicationId))
            throw new UnauthorizedAccessException("Access denied.");

        patient.Deactivate();
        repository.Update(patient);
        await repository.SaveChangesAsync(cancellationToken);
    }
}
