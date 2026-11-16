using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.IpdRegistrations.Commands;
using TenantCore.Application.Features.IpdRegistrations.Translators;
using TenantCore.Application.Features.UsageBilling.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;
using TenantCore.Shared.Errors;

namespace TenantCore.Application.Features.IpdRegistrations.Handlers;

public sealed class CreateIpdRegistrationHandler(
    IIpdRegistrationRepository ipdRepository,
    IPatientRepository patientRepository,
    IBedRepository bedRepository,
    IIpdBedTransferHistoryRepository historyRepository,
    IClinicFeatureFlagsRepository featureFlagsRepository,
    IIpdPaymentRepository paymentRepository,
    ISender sender,
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

        var flags = await featureFlagsRepository.GetByApplicationAsync(request.ApplicationId, cancellationToken);
        var billingEnabled = flags?.BillingEnabled ?? true;

        Bed? bed = null;
        if (request.BedId.HasValue)
        {
            // Confirmed decision: no default billing policy — a ward-only admission with no
            // BedId is never blocked (no room charge applies yet), but a real bed allocation is.
            if (flags?.IpdRoomBillingCycle is null || flags.IpdMidStayChargeMode is null)
                throw new InvalidOperationException(UserMessages.IpdBillingSettingsNotConfigured);

            bed = await bedRepository.GetByIdAsync(request.BedId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(Bed), request.BedId.Value);

            if (bed.ApplicationId != request.ApplicationId || !bed.IsActive)
                throw new NotFoundException(nameof(Bed), request.BedId.Value);

            if (bed.IsOccupied)
                throw new InvalidOperationException(UserMessages.BedAlreadyOccupied);

            bed.MarkOccupied();
        }

        var admissionNumber = await ipdRepository.GetNextAdmissionNumberAsync(request.ApplicationId, cancellationToken);

        var registration = IpdRegistration.Create(
            request.ApplicationId, request.PatientId, request.DoctorUserId,
            request.DoctorName, admissionNumber, bed?.Id,
            bed?.Ward.Name, bed?.Room.RoomNumber, bed?.BedNumber,
            request.InitialFee, request.AdmissionNotes);

        await ipdRepository.AddAsync(registration, cancellationToken);

        if (bed is not null)
        {
            // No separate "admitting staff" id exists on this command yet — DoctorUserId is the
            // closest available actor for this first segment's audit trail.
            var segment = IpdBedTransferHistory.Open(
                request.ApplicationId, registration.Id, bed.Id, bed.RoomId, bed.WardId,
                bed.Room.PricePerDay, request.DoctorUserId, null);
            await historyRepository.AddAsync(segment, cancellationToken);
        }

        if (billingEnabled)
        {
            var payment = IpdPayment.Create(request.ApplicationId, registration.Id, request.InitialFee);
            await paymentRepository.AddAsync(payment, cancellationToken);
        }

        // Metered admission. Dispatched BEFORE the SaveChanges so the admission, its usage row and
        // any wallet debit are one transaction — a refusal commits nothing. The same IpdRegistration
        // Id later appears again as IpdDischarge, which the unique index
        // (ApplicationId, BillingItem, SourceEntityId) permits by design.
        await sender.Send(new ChargeUsageCommand(
            request.ApplicationId, BillingItem.IpdAdmission, registration.Id,
            Quantity: 1, UserId: null, OccurredAt: null), cancellationToken);

        await ipdRepository.SaveChangesAsync(cancellationToken);

        var loaded = await ipdRepository.GetByIdAsync(registration.Id, cancellationToken);
        return IpdRegistrationTranslator.ToDto(loaded!);
    }
}
