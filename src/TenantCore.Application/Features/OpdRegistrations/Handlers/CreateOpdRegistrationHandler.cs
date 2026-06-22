using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.OpdPayments.Commands;
using TenantCore.Application.Features.OpdRegistrations.Commands;
using TenantCore.Application.Features.OpdRegistrations.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdRegistrations.Handlers;

public sealed class CreateOpdRegistrationHandler(
    IOpdRegistrationRepository opdRepository,
    IPatientRepository patientRepository,
    IClinicFeeConfigRepository feeRepository,
    IDoctorProfileRepository doctorProfileRepository,
    ICounterSessionRepository counterSessionRepository,
    ISender sender,
    ILogger<CreateOpdRegistrationHandler> logger)
    : IRequestHandler<CreateOpdRegistrationCommand, OpdRegistrationDto>
{
    public async Task<OpdRegistrationDto> Handle(
        CreateOpdRegistrationCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating OPD registration for patient {PatientId}", request.PatientId);

        var activeSession = await counterSessionRepository.GetActiveSessionAsync(request.ApplicationId, cancellationToken);
        if (activeSession is null)
            throw new InvalidOperationException("No active counter session. Please open a counter session before booking OPD appointments.");

        var patient = await patientRepository.GetByIdAsync(request.PatientId, cancellationToken)
            ?? throw new NotFoundException(nameof(Patient), request.PatientId);

        var fee = request.Fee;
        if (fee is null)
        {
            var config = await feeRepository.GetByApplicationAsync(request.ApplicationId, cancellationToken);
            fee = config?.OpdFee ?? 0m;
        }

        var todayCount = await opdRepository.CountTodayAsync(request.ApplicationId, cancellationToken);
        var registrationNumber = $"OPD-{DateTime.Now:yyyyMMdd}-{(todayCount + 1):D4}";

        var registration = OpdRegistration.Create(
            request.ApplicationId, request.PatientId, request.DoctorUserId,
            request.DoctorName, registrationNumber, fee.Value, request.Notes,
            request.Weight, request.BloodPressure, request.PulseRate, request.OxygenSaturation);

        await opdRepository.AddAsync(registration, cancellationToken);
        await opdRepository.SaveChangesAsync(cancellationToken);

        var doctorProfile = await doctorProfileRepository.GetByUserIdAsync(request.DoctorUserId, cancellationToken);
        var doctorProfileId = doctorProfile?.Id ?? Guid.Empty;
        await sender.Send(new EnsureOpdPaymentCommand(registration.Id, doctorProfileId, request.ApplicationId), cancellationToken);

        var loaded = await opdRepository.GetByIdAsync(registration.Id, cancellationToken);
        return OpdRegistrationTranslator.ToDto(loaded!);
    }
}
