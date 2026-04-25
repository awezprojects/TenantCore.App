using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Patients.Commands;
using TenantCore.Application.Features.Patients.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Patients.Handlers;

public sealed class RegisterPatientHandler(
    IPatientRepository repository,
    ILogger<RegisterPatientHandler> logger)
    : IRequestHandler<RegisterPatientCommand, PatientDto>
{
    public async Task<PatientDto> Handle(RegisterPatientCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Registering patient {FirstName} {LastName} for application {ApplicationId}",
            request.FirstName, request.LastName, request.ApplicationId);

        var patient = Patient.Create(
            request.ApplicationId, request.FirstName, request.LastName,
            request.DateOfBirth, request.Gender, request.PhoneNumber,
            request.Email, request.AadhaarNumber, request.PhotoUrl, request.Address);

        await repository.AddAsync(patient, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return PatientTranslator.ToDto(patient, request.ShowFullAadhaar);
    }
}
