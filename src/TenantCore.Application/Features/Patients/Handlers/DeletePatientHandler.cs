using MediatR;
using TenantCore.Application.Features.Patients.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.Patients.Handlers;

public sealed class DeletePatientHandler(IPatientRepository repository)
    : IRequestHandler<DeletePatientCommand>
{
    public async Task Handle(DeletePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Patient), request.Id);

        if (patient.ApplicationId != request.ApplicationId)
            throw new UnauthorizedAccessException("Access denied.");

        patient.Deactivate();
        repository.Update(patient);
        await repository.SaveChangesAsync(cancellationToken);
    }
}
