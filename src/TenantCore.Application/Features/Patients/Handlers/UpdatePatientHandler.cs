using MediatR;
using TenantCore.Application.Features.Patients.Commands;
using TenantCore.Application.Features.Patients.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Patients.Handlers;

public sealed class UpdatePatientHandler(IPatientRepository repository)
    : IRequestHandler<UpdatePatientCommand, PatientDto>
{
    public async Task<PatientDto> Handle(UpdatePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Patient), request.Id);

        if (patient.ApplicationId != request.ApplicationId)
            throw new UnauthorizedAccessException("Access denied.");

        patient.Update(
            request.FirstName, request.LastName, request.DateOfBirth,
            request.Gender, request.PhoneNumber, request.Email,
            request.AadhaarNumber, request.PhotoUrl, request.Address);

        repository.Update(patient);
        await repository.SaveChangesAsync(cancellationToken);

        return PatientTranslator.ToDto(patient, request.ShowFullAadhaar);
    }
}
