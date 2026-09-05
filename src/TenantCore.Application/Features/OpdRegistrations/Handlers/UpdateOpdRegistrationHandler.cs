using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.OpdRegistrations.Commands;
using TenantCore.Application.Features.OpdRegistrations.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdRegistrations.Handlers;

public sealed class UpdateOpdRegistrationHandler(IOpdRegistrationRepository repository, IApplicationAccessValidator accessValidator)
    : IRequestHandler<UpdateOpdRegistrationCommand, OpdRegistrationDto>
{
    public async Task<OpdRegistrationDto> Handle(
        UpdateOpdRegistrationCommand request, CancellationToken cancellationToken)
    {
        var registration = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(OpdRegistration), request.Id);

        if (!accessValidator.CanAccess(registration.ApplicationId))
            throw new UnauthorizedAccessException("Access denied.");

        if (request.DoctorUserId != registration.DoctorUserId)
        {
            var alreadyRegisteredToday = await repository.ExistsForPatientDoctorOnDateAsync(
                registration.ApplicationId, registration.PatientId, request.DoctorUserId, registration.RegistrationDate, cancellationToken);
            if (alreadyRegisteredToday)
                throw new InvalidOperationException(
                    $"This patient already has an OPD appointment with {request.DoctorName} on this date.");
        }

        // Update() takes vitals as explicit params (no partial-update support) — pass the
        // registration's own current values through so this status/doctor/fee edit doesn't
        // blank out the vitals captured at registration time.
        registration.Update(request.DoctorUserId, request.DoctorName, request.Fee, request.Status, request.Notes,
            registration.Weight, registration.BloodPressure, registration.PulseRate, registration.OxygenSaturation,
            registration.Temperature, registration.RespiratoryRate, registration.Sugar);
        repository.Update(registration);
        await repository.SaveChangesAsync(cancellationToken);

        return OpdRegistrationTranslator.ToDto(registration);
    }
}
