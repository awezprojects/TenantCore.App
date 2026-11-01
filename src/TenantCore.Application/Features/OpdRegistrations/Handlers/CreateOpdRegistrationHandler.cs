using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.OpdPayments.Commands;
using TenantCore.Application.Features.OpdRegistrations.Commands;
using TenantCore.Application.Features.OpdRegistrations.Translators;
using TenantCore.Application.Features.UsageBilling.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.OpdRegistrations.Handlers;

public sealed class CreateOpdRegistrationHandler(
    IOpdRegistrationRepository opdRepository,
    IPatientRepository patientRepository,
    IClinicFeeConfigRepository feeRepository,
    IClinicFeatureFlagsRepository featureFlagsRepository,
    IDoctorProfileRepository doctorProfileRepository,
    ICounterSessionRepository counterSessionRepository,
    IOpdPaymentRepository paymentRepository,
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

        var alreadyRegisteredToday = await opdRepository.ExistsForPatientDoctorOnDateAsync(
            request.ApplicationId, request.PatientId, request.DoctorUserId, DateTime.Now, cancellationToken);
        if (alreadyRegisteredToday)
            throw new InvalidOperationException(
                $"{patient.FirstName} {patient.LastName} already has an OPD appointment with {request.DoctorName} today.");

        var fee = request.Fee;
        if (fee is null)
        {
            var config = await feeRepository.GetByApplicationAsync(request.ApplicationId, cancellationToken);
            fee = config?.OpdFee ?? 0m;
        }

        var registrationNumber = await opdRepository.GetNextRegistrationNumberAsync(request.ApplicationId, cancellationToken);

        var registration = OpdRegistration.Create(
            request.ApplicationId, request.PatientId, request.DoctorUserId,
            request.DoctorName, registrationNumber, fee.Value, request.Notes,
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

        if (billingEnabled)
        {
            var doctorProfile = await doctorProfileRepository.GetByUserIdAsync(request.DoctorUserId, cancellationToken);
            var doctorProfileId = doctorProfile?.Id ?? Guid.Empty;
            await sender.Send(new EnsureOpdPaymentCommand(registration.Id, doctorProfileId, request.ApplicationId), cancellationToken);

            if (flags?.PrepaidOpdEnabled ?? true)
            {
                var payment = await paymentRepository.GetByOpdRegistrationIdAsync(registration.Id, request.ApplicationId, cancellationToken);
                if (payment is not null && payment.PaymentStatus == PaymentStatus.Pending)
                {
                    payment.AcceptVisitFee(request.ReceivedByUserId, activeSession!.Id);
                    paymentRepository.Update(payment);
                    await paymentRepository.SaveChangesAsync(cancellationToken);
                }
            }
        }

        var loaded = await opdRepository.GetByIdAsync(registration.Id, cancellationToken);
        return OpdRegistrationTranslator.ToDto(loaded!);
    }
}
