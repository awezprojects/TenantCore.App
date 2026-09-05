using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.IpdRegistrations.Commands;
using TenantCore.Application.Features.IpdRegistrations.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Errors;

namespace TenantCore.Application.Features.IpdRegistrations.Handlers;

public sealed class CreateIpdRegistrationHandler(
    IIpdRegistrationRepository ipdRepository,
    IPatientRepository patientRepository,
    IBedRepository bedRepository,
    ILogger<CreateIpdRegistrationHandler> logger)
    : IRequestHandler<CreateIpdRegistrationCommand, IpdRegistrationDto>
{
    public async Task<IpdRegistrationDto> Handle(
        CreateIpdRegistrationCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating IPD registration for patient {PatientId}", request.PatientId);

        _ = await patientRepository.GetByIdAsync(request.PatientId, cancellationToken)
            ?? throw new NotFoundException(nameof(Patient), request.PatientId);

        if (await ipdRepository.HasActiveAdmissionAsync(request.PatientId, request.ApplicationId, cancellationToken))
            throw new InvalidOperationException(UserMessages.PatientAlreadyAdmitted);

        Bed? bed = null;
        if (!string.IsNullOrWhiteSpace(request.WardName) &&
            !string.IsNullOrWhiteSpace(request.RoomNumber) &&
            !string.IsNullOrWhiteSpace(request.BedNumber))
        {
            bed = await bedRepository.FindByLocationAsync(
                request.ApplicationId, request.WardName, request.RoomNumber, request.BedNumber, cancellationToken);

            if (bed != null && bed.IsOccupied)
                throw new InvalidOperationException(UserMessages.BedAlreadyOccupied);

            bed?.MarkOccupied();
        }

        var admissionNumber = await ipdRepository.GetNextAdmissionNumberAsync(request.ApplicationId, cancellationToken);

        var registration = IpdRegistration.Create(
            request.ApplicationId, request.PatientId, request.DoctorUserId,
            request.DoctorName, admissionNumber, request.WardName,
            request.RoomNumber, request.BedNumber, request.InitialFee, request.AdmissionNotes);

        await ipdRepository.AddAsync(registration, cancellationToken);
        await ipdRepository.SaveChangesAsync(cancellationToken);

        var loaded = await ipdRepository.GetByIdAsync(registration.Id, cancellationToken);
        return IpdRegistrationTranslator.ToDto(loaded!);
    }
}
