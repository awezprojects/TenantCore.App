using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.OpdRegistrations.Commands;
using TenantCore.Application.Features.OpdRegistrations.Translators;
using TenantCore.Application.Features.UsageBilling.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.OpdRegistrations.Handlers;

public sealed class UpdateOpdRegistrationHandler(
    IOpdRegistrationRepository repository,
    IOpdPaymentRepository paymentRepository,
    IApplicationAccessValidator accessValidator,
    ISender sender)
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

        // DEF-OPD-04/05/09: a visit that is cancelled here, for the first time, never happened —
        // so (a) the platform usage charge is reversed exactly once, in cash or not, and (b)
        // whatever was collected from the patient is owed back in full, even over an earlier
        // partial-discount refund. Checked against the registration's *current* status so a
        // no-op PUT (already Cancelled → Cancelled) can never fire this twice.
        var cancellingNow = request.Status == OpdStatus.Cancelled && registration.Status != OpdStatus.Cancelled;

        // Update() takes vitals as explicit params (no partial-update support) — pass the
        // registration's own current values through so this status/doctor/fee edit doesn't
        // blank out the vitals captured at registration time.
        registration.Update(request.DoctorUserId, request.DoctorName, request.Fee, request.Status, request.Notes,
            registration.Weight, registration.BloodPressure, registration.PulseRate, registration.OxygenSaturation,
            registration.Temperature, registration.RespiratoryRate, registration.Sugar);
        repository.Update(registration);

        if (cancellingNow)
        {
            await sender.Send(new ReverseUsageChargeCommand(
                registration.ApplicationId, BillingItem.OpdRegistration, registration.Id,
                request.ActingUserId, OccurredAt: null), cancellationToken);

            var payment = await paymentRepository.GetByOpdRegistrationIdAsync(
                registration.Id, registration.ApplicationId, cancellationToken);
            if (payment is not null)
            {
                payment.CancelForFullRefund();
                paymentRepository.Update(payment);
            }
        }

        await repository.SaveChangesAsync(cancellationToken);

        return OpdRegistrationTranslator.ToDto(registration);
    }
}
