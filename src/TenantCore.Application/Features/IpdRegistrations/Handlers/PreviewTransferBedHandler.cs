using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdPayments.Services;
using TenantCore.Application.Features.IpdRegistrations.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;
using TenantCore.Shared.Errors;

namespace TenantCore.Application.Features.IpdRegistrations.Handlers;

public sealed class PreviewTransferBedHandler(
    IIpdRegistrationRepository ipdRepository,
    IBedRepository bedRepository,
    IIpdBedTransferHistoryRepository historyRepository,
    IClinicFeatureFlagsRepository featureFlagsRepository,
    IIpdBillCalculator billCalculator,
    IApplicationAccessValidator accessValidator)
    : IRequestHandler<PreviewTransferBedQuery, TransferBedPreviewDto>
{
    public async Task<TransferBedPreviewDto> Handle(PreviewTransferBedQuery request, CancellationToken cancellationToken)
    {
        var registration = await ipdRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(IpdRegistration), request.Id);

        if (!accessValidator.CanAccess(registration.ApplicationId))
            throw new UnauthorizedAccessException("Access denied.");

        // See TransferBedHandler for why this is checked again here, scoped to the registration's
        // real clinic, rather than trusting the controller's [Authorize(Policy=RequireReception)]
        // alone for this route shape.
        if (!accessValidator.HasRole(registration.ApplicationId, AppRoles.ReceptionRoles))
            throw new UnauthorizedAccessException("Access denied.");

        // SECURITY: every lookup below is scoped by registration.ApplicationId — the
        // registration's OWN clinic — never request.ApplicationId (the caller-supplied
        // X-Application-Id header). CanAccess above only proves the caller holds *some* role in
        // registration.ApplicationId; a user with roles in two clinics could otherwise select
        // clinic A and have this preview read clinic A's billing settings and bed/room rate while
        // looking at a clinic B admission — a cross-tenant information disclosure (see the
        // newBed.ApplicationId check below for the other half of this).
        var flags = await featureFlagsRepository.GetByApplicationAsync(registration.ApplicationId, cancellationToken);
        if (flags?.IpdRoomBillingCycle is null || flags.IpdMidStayChargeMode is null)
            throw new InvalidOperationException(UserMessages.IpdBillingSettingsNotConfigured);

        var newBed = await bedRepository.GetByIdAsync(request.NewBedId, cancellationToken)
            ?? throw new NotFoundException(nameof(Bed), request.NewBedId);

        // Without this check, any authenticated clinic user could pass another clinic's BedId and
        // have this endpoint hand back that clinic's room rate (via StandardAmount/
        // SuggestedAmount) — a cross-tenant information disclosure, and also a bed-id enumeration
        // oracle. Also rejects an inactive bed, matching TransferBedHandler's own gate exactly —
        // this read-only preview must not be a weaker check than the mutating action it previews.
        if (newBed.ApplicationId != registration.ApplicationId || !newBed.IsActive)
            throw new NotFoundException(nameof(Bed), request.NewBedId);

        var currentSegment = await historyRepository.GetCurrentSegmentAsync(registration.Id, registration.ApplicationId, cancellationToken);
        if (currentSegment is null)
        {
            // Allocating the first bed, not shifting one — nothing to preview against.
            return new TransferBedPreviewDto(0m, 0m, 0m, false, false);
        }

        var now = DateTime.UtcNow;
        var standardCharge = billCalculator.CalculateStandardSegmentCharge(currentSegment, now, flags.IpdMidStayChargeMode.Value);

        var withinGrace = flags.IpdShiftGraceWindowMinutes > 0 && flags.IpdShiftGraceRule.HasValue &&
                          now - currentSegment.OccupiedFrom <= TimeSpan.FromMinutes(flags.IpdShiftGraceWindowMinutes);

        var suggestedAmount = standardCharge;
        if (withinGrace)
        {
            var oldRoomStandardCharge = currentSegment.RatePerDayAtTime;
            var newRoomStandardCharge = newBed.Room.PricePerDay;
            suggestedAmount = flags.IpdShiftGraceRule!.Value switch
            {
                IpdShiftGraceRule.NewRoomOnly => newRoomStandardCharge,
                IpdShiftGraceRule.LowerRate => Math.Min(oldRoomStandardCharge, newRoomStandardCharge),
                _ => oldRoomStandardCharge
            };
        }

        var manualAllowed = flags.IpdManualShiftChargeMode switch
        {
            IpdManualShiftChargeMode.Off => false,
            IpdManualShiftChargeMode.AdminOnly => accessValidator.HasRole(registration.ApplicationId, [AppRoles.ClinicAdmin]),
            _ => true // AdminAndReception — caller already holds a reception-capable role to reach Shift Bed at all
        };

        return new TransferBedPreviewDto(standardCharge, suggestedAmount, standardCharge, withinGrace, manualAllowed);
    }
}
