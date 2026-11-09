using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.OpdPayments.Commands;
using TenantCore.Application.Features.OpdRegistrations.Commands;
using TenantCore.Application.Features.OpdRegistrations.Translators;
using TenantCore.Application.Features.UsageBilling.Commands;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.OpdRegistrations.Handlers;

public sealed class CreateOpdRegistrationHandler(
    IOpdRegistrationRepository opdRepository,
    IPatientRepository patientRepository,
    IClinicFeeConfigRepository feeRepository,
    IDoctorFeeConfigRepository doctorFeeConfigRepository,
    IClinicFeatureFlagsRepository featureFlagsRepository,
    IDoctorProfileRepository doctorProfileRepository,
    ICounterSessionRepository counterSessionRepository,
    IOpdPaymentRepository paymentRepository,
    IAuthApplicationService authApplicationService,
    ISender sender,
    ILogger<CreateOpdRegistrationHandler> logger)
    : IRequestHandler<CreateOpdRegistrationCommand, OpdRegistrationDto>
{
    public async Task<OpdRegistrationDto> Handle(
        CreateOpdRegistrationCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating OPD registration for patient {PatientId}", request.PatientId);

        var flags = await featureFlagsRepository.GetByApplicationAsync(request.ApplicationId, cancellationToken);
        var billingEnabled = flags?.BillingEnabled ?? true;

        CounterSession? activeSession = null;
        if (billingEnabled)
        {
            activeSession = await counterSessionRepository.GetActiveSessionAsync(request.ApplicationId, cancellationToken);
            if (activeSession is null)
                throw new InvalidOperationException("No active counter session. Please open a counter session before booking OPD appointments.");
        }

        var patient = await patientRepository.GetByIdAsync(request.PatientId, cancellationToken)
            ?? throw new NotFoundException(nameof(Patient), request.PatientId);

        // DEF-OPD-06: the client-sent DoctorUserId/DoctorName were never checked against the
        // clinic's own staff, so a random id and a made-up name created an orphan visit no real
        // doctor could ever see. Resolve the doctor server-side and take the name from there.
        var doctorProfile = await doctorProfileRepository.GetByUserIdAsync(request.DoctorUserId, cancellationToken);
        var staff = await authApplicationService.GetClinicStaffDirectoryAsync(request.ApplicationId, roles: null, cancellationToken);
        var doctorEntry = staff.FirstOrDefault(u => u.UserId == request.DoctorUserId);
        var isClinicDoctor = doctorEntry is not null &&
            (doctorProfile is not null
             || doctorEntry.RoleNames.Exists(r => string.Equals(r, AppRoles.Doctor, StringComparison.OrdinalIgnoreCase))
             || string.Equals(doctorEntry.RoleName, AppRoles.Doctor, StringComparison.OrdinalIgnoreCase));
        if (!isClinicDoctor)
            throw new InvalidOperationException("The selected doctor is not a member of this clinic.");

        var doctorName = doctorEntry!.FullName;

        var alreadyRegisteredToday = await opdRepository.ExistsForPatientDoctorOnDateAsync(
            request.ApplicationId, request.PatientId, request.DoctorUserId, DateTime.Now, cancellationToken);
        if (alreadyRegisteredToday)
            throw new InvalidOperationException(
                $"{patient.FirstName} {patient.LastName} already has an OPD appointment with {doctorName} today.");

        // Fee resolution order: an explicit per-visit fee from reception always wins (deliberate
        // override); otherwise this doctor's own configured rate (DoctorFeeConfig) applies; only
        // when neither exists does the clinic-wide default (ClinicFeeConfig.OpdFee) apply.
        // DoctorFeeConfig used to be set here but never read anywhere — admins configuring a
        // per-doctor rate had no effect on what was actually charged.
        var fee = request.Fee;
        if (fee is null && doctorProfile is not null)
        {
            var doctorFeeConfig = await doctorFeeConfigRepository.GetByDoctorProfileIdAsync(
                doctorProfile.Id, request.ApplicationId, cancellationToken);
            if (doctorFeeConfig is { IsActive: true })
                fee = doctorFeeConfig.VisitFee;
        }
        if (fee is null)
        {
            var config = await feeRepository.GetByApplicationAsync(request.ApplicationId, cancellationToken);
            fee = config?.OpdFee ?? 0m;
        }

        var registrationNumber = await opdRepository.GetNextRegistrationNumberAsync(request.ApplicationId, cancellationToken);

        var registration = OpdRegistration.Create(
            request.ApplicationId, request.PatientId, request.DoctorUserId,
            doctorName, registrationNumber, fee.Value, request.Notes,
            request.Weight, request.BloodPressure, request.PulseRate, request.OxygenSaturation,
            request.Temperature, request.RespiratoryRate, request.Sugar);

        await opdRepository.AddAsync(registration, cancellationToken);

        // Platform billing is deliberately NOT the clinic's patient-billing module, so this sits
        // outside the billingEnabled block below. Dispatched BEFORE the SaveChanges, so the
        // registration, its usage row and any wallet debit commit as one transaction: a refusal
        // commits nothing and never leaves an operation that exists but was never billed.
        // OpdRegistration.Create assigns Id before the insert, so registration.Id is already valid.
        await sender.Send(new ChargeUsageCommand(
            request.ApplicationId, BillingItem.OpdRegistration, registration.Id,
            Quantity: 1, UserId: request.ReceivedByUserId, OccurredAt: null), cancellationToken);

        await opdRepository.SaveChangesAsync(cancellationToken);

        // DEF-MR-05: carried through to the DTO below so the create response (and therefore the
        // just-created row shown in the OPD list before any reload) reports who registered the
        // visit. Stays null when prepaid is disabled — nothing was collected, so there is no
        // "received by" yet.
        OpdPayment? payment = null;
        if (billingEnabled)
        {
            var doctorProfileId = doctorProfile?.Id ?? Guid.Empty;
            await sender.Send(new EnsureOpdPaymentCommand(registration.Id, doctorProfileId, request.ApplicationId), cancellationToken);

            if (flags?.PrepaidOpdEnabled ?? true)
            {
                payment = await paymentRepository.GetByOpdRegistrationIdAsync(registration.Id, request.ApplicationId, cancellationToken);
                if (payment is not null && payment.PaymentStatus == PaymentStatus.Pending)
                {
                    payment.AcceptVisitFee(request.ReceivedByUserId, activeSession!.Id);
                    paymentRepository.Update(payment);
                    await paymentRepository.SaveChangesAsync(cancellationToken);
                }
            }
        }

        var loaded = await opdRepository.GetByIdAsync(registration.Id, cancellationToken);
        return OpdRegistrationTranslator.ToDto(loaded!, payment);
    }
}
