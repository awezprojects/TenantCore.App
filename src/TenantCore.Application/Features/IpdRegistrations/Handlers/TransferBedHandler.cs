using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Common.Exceptions;
using TenantCore.Application.Features.IpdPayments.Services;
using TenantCore.Application.Features.IpdRegistrations.Commands;
using TenantCore.Application.Features.IpdRegistrations.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;
using TenantCore.Shared.Errors;

namespace TenantCore.Application.Features.IpdRegistrations.Handlers;

// Fixes DEF-IPD-01/02/03: a dedicated command that validates the target bed, moves occupancy
// atomically (old bed freed, new bed occupied, one SaveChanges) and stores a real Bed id —
// the generic Update endpoint never touched a Bed entity at all.
//
// DEF-IPD-16 (13-Nov): clinic-specific shift charging. When the clinic has configured a grace
// window and the shift happens inside it, or the caller supplied a manual override amount, the
// closing segment's room charge for the shift day is frozen here (IpdBedTransferHistory.FreezeCharge)
// instead of being left to the plain multi-segment calculator — see IpdBillCalculator for how a
// frozen segment settles its whole block.
public sealed class TransferBedHandler(
    IIpdRegistrationRepository ipdRepository,
    IBedRepository bedRepository,
    IIpdBedTransferHistoryRepository historyRepository,
    IClinicFeatureFlagsRepository featureFlagsRepository,
    IIpdBillCalculator billCalculator,
    IApplicationAccessValidator accessValidator)
    : IRequestHandler<TransferBedCommand, IpdRegistrationDto>
{
    public async Task<IpdRegistrationDto> Handle(TransferBedCommand request, CancellationToken cancellationToken)
    {
        var registration = await ipdRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(IpdRegistration), request.Id);

        if (!accessValidator.CanAccess(registration.ApplicationId))
            throw new UnauthorizedAccessException("Access denied.");

        // SECURITY: the controller's [Authorize(Policy = RequireReception)] resolves its clinic
        // from the X-Application-Id header for this route shape ({id:guid}/transfer-bed has no
        // "applicationId" route segment — see ClinicRoleAuthorizationHandler) — so for a user who
        // holds a non-reception role (e.g. Doctor) in registration's real clinic but a
        // reception-capable role in some OTHER clinic they also belong to, that policy can pass
        // on the wrong clinic's role. This re-checks the role specifically against
        // registration.ApplicationId, which CanAccess alone does not (it only proves linkage, not
        // which role).
        if (!accessValidator.HasRole(registration.ApplicationId, AppRoles.ReceptionRoles))
            throw new UnauthorizedAccessException("Access denied.");

        if (registration.Status != IpdStatus.Admitted)
            throw new InvalidOperationException(UserMessages.IpdNotAdmitted);

        // SECURITY: every lookup below is scoped by registration.ApplicationId — the
        // registration's OWN clinic, fetched from the entity — never request.ApplicationId (the
        // caller-supplied X-Application-Id header). CanAccess above only proves the caller holds
        // *some* role in registration.ApplicationId; a user who holds roles in two clinics could
        // otherwise select clinic A, target a clinic B admission by id, and have this handler
        // validate the target bed and read billing settings against clinic A while mutating a
        // clinic B record — a cross-tenant data-corruption path, not merely a read leak.
        var flags = await featureFlagsRepository.GetByApplicationAsync(registration.ApplicationId, cancellationToken);
        if (flags?.IpdRoomBillingCycle is null || flags.IpdMidStayChargeMode is null)
            throw new InvalidOperationException(UserMessages.IpdBillingSettingsNotConfigured);

        var newBed = await bedRepository.GetByIdAsync(request.NewBedId, cancellationToken)
            ?? throw new NotFoundException(nameof(Bed), request.NewBedId);

        if (newBed.ApplicationId != registration.ApplicationId || !newBed.IsActive)
            throw new NotFoundException(nameof(Bed), request.NewBedId);

        // OBS-5: checked before IsOccupied, which is otherwise always true for the patient's
        // own current bed and produces the misleading "already occupied" message.
        if (registration.CurrentBedId == request.NewBedId)
            throw new InvalidOperationException(UserMessages.IpdAlreadyInThisBed);

        if (newBed.IsOccupied)
            throw new InvalidOperationException(UserMessages.BedAlreadyOccupied);

        var currentSegment = await historyRepository.GetCurrentSegmentAsync(registration.Id, registration.ApplicationId, cancellationToken);
        if (currentSegment is not null)
        {
            var now = DateTime.UtcNow;
            var standardCharge = billCalculator.CalculateStandardSegmentCharge(currentSegment, now, flags.IpdMidStayChargeMode.Value);

            if (request.ManualChargeAmount.HasValue)
            {
                ValidateManualCharge(request, flags, standardCharge, registration.ApplicationId, accessValidator);
                currentSegment.SetManualCharge(request.ManualChargeAmount.Value, request.ManualChargeReason!, request.TransferredByUserId);
                currentSegment.FreezeCharge(standardCharge, request.ManualChargeAmount.Value, IpdBillAdjustmentType.Manual);
            }
            else if (flags.IpdShiftGraceWindowMinutes > 0 && flags.IpdShiftGraceRule.HasValue &&
                     now - currentSegment.OccupiedFrom <= TimeSpan.FromMinutes(flags.IpdShiftGraceWindowMinutes))
            {
                // Grace is a whole-day comparison (same framing as the worked example: ICU's full
                // day rate vs. General's full day rate), independent of the clinic's Prorated/
                // FullDayCharge setting — unlike the manual-cap case above, which is deliberately
                // "for the time actually spent in that room."
                var oldRoomStandardCharge = currentSegment.RatePerDayAtTime;
                var newRoomStandardCharge = newBed.Room.PricePerDay;

                var appliedCharge = flags.IpdShiftGraceRule.Value switch
                {
                    IpdShiftGraceRule.NewRoomOnly => newRoomStandardCharge,
                    IpdShiftGraceRule.LowerRate => Math.Min(oldRoomStandardCharge, newRoomStandardCharge),
                    _ => oldRoomStandardCharge // OldRoomOnly
                };
                currentSegment.FreezeCharge(oldRoomStandardCharge, appliedCharge, IpdBillAdjustmentType.Grace);
            }

            var oldBed = await bedRepository.GetByIdAsync(currentSegment.BedId, cancellationToken);
            oldBed?.MarkAvailable();
            currentSegment.Close();
            historyRepository.Update(currentSegment);
        }
        else if (request.ManualChargeAmount.HasValue)
        {
            // No current segment to charge against (a ward-only admission allocating its first
            // bed isn't a "shift") — a manual amount here has nothing to apply to.
            throw new DomainValidationException("There is no current room charge to override.");
        }

        newBed.MarkOccupied();

        var newSegment = IpdBedTransferHistory.Open(
            registration.ApplicationId, registration.Id, newBed.Id, newBed.RoomId, newBed.WardId,
            newBed.Room.PricePerDay, request.TransferredByUserId, request.Notes);
        await historyRepository.AddAsync(newSegment, cancellationToken);

        registration.RecordBedTransfer(newBed.Id, newBed.Ward.Name, newBed.Room.RoomNumber, newBed.BedNumber);
        ipdRepository.Update(registration);

        // All four repositories above share this request's ClinicDbContext — one SaveChanges
        // commits old-bed-freed + new-bed-occupied + history close/open + registration pointer
        // together, same cross-repository-single-commit pattern CreateIpdRegistrationHandler uses.
        await ipdRepository.SaveChangesAsync(cancellationToken);

        var loaded = await ipdRepository.GetByIdAsync(registration.Id, cancellationToken);
        return IpdRegistrationTranslator.ToDto(loaded!);
    }

    private static void ValidateManualCharge(
        TransferBedCommand request, ClinicFeatureFlags flags, decimal standardCharge,
        Guid applicationId, IApplicationAccessValidator accessValidator)
    {
        if (flags.IpdManualShiftChargeMode == IpdManualShiftChargeMode.Off)
            throw new ErrorCodeException(403, "manual_shift_charge_not_allowed", "Manual shift charges are turned off for this clinic.");

        // SECURITY: checked against applicationId (the registration's real clinic), via
        // accessValidator — never against a client-supplied role list, and never against
        // whatever clinic the X-Application-Id header happens to say (see the HasRole call above
        // in Handle for why that distinction matters here).
        if (flags.IpdManualShiftChargeMode == IpdManualShiftChargeMode.AdminOnly &&
            !accessValidator.HasRole(applicationId, [AppRoles.ClinicAdmin]))
        {
            throw new ErrorCodeException(403, "manual_shift_charge_not_allowed", "Only a Clinic Admin may enter a manual shift charge for this clinic.");
        }

        if (request.ManualChargeAmount!.Value > standardCharge)
            throw new DomainValidationException($"The manual charge amount cannot exceed the standard charge of ₹{standardCharge:N2}.");
    }
}
