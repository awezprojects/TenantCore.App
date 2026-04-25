using MediatR;
using TenantCore.Application.Features.Patients.Queries;
using TenantCore.Application.Features.Patients.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Patients.Handlers;

public sealed class GetPatientByIdHandler(IPatientRepository repository)
    : IRequestHandler<GetPatientByIdQuery, PatientDto>
{
    public async Task<PatientDto> Handle(GetPatientByIdQuery request, CancellationToken cancellationToken)
    {
        var patient = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Patient), request.Id);

        if (patient.ApplicationId != request.ApplicationId)
            throw new UnauthorizedAccessException("Access denied.");

        return PatientTranslator.ToDto(patient, request.ShowFullAadhaar);
    }
}
