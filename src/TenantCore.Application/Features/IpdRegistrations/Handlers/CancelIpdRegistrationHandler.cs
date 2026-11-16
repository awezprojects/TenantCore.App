using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdRegistrations.Commands;
using TenantCore.Application.Features.IpdRegistrations.Translators;
using TenantCore.Application.Features.UsageBilling.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.IpdRegistrations.Handlers;

// DEF-IPD-19: a cancelled admission is a mistake being undone, not a stay that happened — so it
// must bill ₹0 (no room charge, no initial fee) and refund every deposit in full, not go through
// the normal FinalizeIpdBillCommand/IpdBillCalculator path a real discharge uses. If service items
// were already added, the cancel is blocked instead of silently dropping them — removing them (or
// discharging instead) is the reception's call, not this handler's to make for them. The admission
// usage charge (BillingItem.IpdAdmission) is reversed too, since the admission itself is being
// undone, not just its billing.
public sealed class CancelIpdRegistrationHandler(
    IIpdRegistrationRepository ipdRepository,
    IBedRepository bedRepository,
    IIpdBedTransferHistoryRepository historyRepository,
    IIpdPaymentRepository paymentRepository,
    IClinicFeatureFlagsRepository featureFlagsRepository,
    IApplicationAccessValidator accessValidator,
    ISender sender)
    : IRequestHandler<CancelIpdRegistrationCommand, IpdRegistrationDto>
{
    public async Task<IpdRegistrationDto> Handle(CancelIpdRegistrationCommand request, CancellationToken cancellationToken)
    {
        var registration = await ipdRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(IpdRegistration), request.Id);

        if (!accessValidator.CanAccess(registration.ApplicationId))
            throw new UnauthorizedAccessException("Access denied.");

        // SECURITY: re-checked against registration.ApplicationId specifically — see
        // TransferBedHandler for the full reasoning. This one moves money (full deposit refund,
        // usage-charge reversal), so it must not be gateable via a reception role held in some
        // other clinic the caller also belongs to.
        if (!accessValidator.HasRole(registration.ApplicationId, AppRoles.ReceptionRoles))
            throw new UnauthorizedAccessException("Access denied.");

        var payment = await paymentRepository.GetByRegistrationAsync(registration.Id, registration.ApplicationId, cancellationToken);
        if (payment is not null && payment.ParticularsTotal > 0m)
            throw new DomainValidationException("Remove the service items or discharge the patient instead.");

        // DEF-IPD-26 (15-Nov): checked before anything else is touched (bed/segment) — a cancel
        // that's about to be refused for being too late must not partially mutate in-memory state
        // first. 0 = unlimited, same as every clinic's cancel policy before this setting existed.
        var flags = await featureFlagsRepository.GetByApplicationAsync(registration.ApplicationId, cancellationToken);
        var cancelledAt = DateTime.UtcNow;
        registration.Cancel(request.Reason, flags?.IpdCancelWindowMinutes ?? 0, cancelledAt);
        ipdRepository.Update(registration);

        var currentSegment = await historyRepository.GetCurrentSegmentAsync(registration.Id, registration.ApplicationId, cancellationToken);
        if (currentSegment is not null)
        {
            var bed = await bedRepository.GetByIdAsync(currentSegment.BedId, cancellationToken);
            bed?.MarkAvailable();
            currentSegment.Close();
            historyRepository.Update(currentSegment);
        }

        if (payment is not null)
        {
            payment.FinalizeAtCancellation(cancelledAt);
            paymentRepository.Update(payment);
        }

        // Internal sub-command, no own SaveChanges — reverses the ₹10 IpdAdmission usage charge in
        // the same transaction as the cancel itself. A no-op (billing disabled, free row, already
        // reversed) is silent, same as every other caller of this command.
        await sender.Send(new ReverseUsageChargeCommand(
            registration.ApplicationId, BillingItem.IpdAdmission, registration.Id,
            request.CancelledByUserId, cancelledAt), cancellationToken);

        await ipdRepository.SaveChangesAsync(cancellationToken);

        return IpdRegistrationTranslator.ToDto(registration);
    }
}
