using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdPayments.Commands;
using TenantCore.Application.Features.IpdRegistrations.Commands;
using TenantCore.Application.Features.IpdRegistrations.Translators;
using TenantCore.Application.Features.UsageBilling.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.IpdRegistrations.Handlers;

public sealed class DischargePatientHandler(
    IIpdRegistrationRepository repository,
    IBedRepository bedRepository,
    IIpdBedTransferHistoryRepository historyRepository,
    IApplicationAccessValidator accessValidator,
    ISender sender)
    : IRequestHandler<DischargePatientCommand, IpdRegistrationDto>
{
    public async Task<IpdRegistrationDto> Handle(
        DischargePatientCommand request, CancellationToken cancellationToken)
    {
        var registration = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(IpdRegistration), request.Id);

        if (!accessValidator.CanAccess(registration.ApplicationId))
            throw new UnauthorizedAccessException("Access denied.");

        // Free the bed via CurrentBedId/the open transfer-history segment — not by re-resolving
        // free-text ward/room/bed strings, which is what left a "ghost" occupied bed behind
        // after a transfer (DEF-IPD-01).
        var currentSegment = await historyRepository.GetCurrentSegmentAsync(registration.Id, registration.ApplicationId, cancellationToken);
        if (currentSegment is not null)
        {
            var bed = await bedRepository.GetByIdAsync(currentSegment.BedId, cancellationToken);
            bed?.MarkAvailable();
            currentSegment.Close();
            historyRepository.Update(currentSegment);
        }

        registration.Discharge(request.DischargeNotes);
        repository.Update(registration);

        var dischargeDate = registration.DischargeDate!.Value;

        // Metered discharge, staged before the save for the same reason as the admission: the
        // discharge, its usage row and any wallet debit are one transaction, so a clinic with an
        // empty wallet cannot leave a patient discharged but un-billed.
        await sender.Send(new ChargeUsageCommand(
            registration.ApplicationId, BillingItem.IpdDischarge, registration.Id,
            Quantity: 1, UserId: null, OccurredAt: null), cancellationToken);

        // Locks the Final Bill as-of the discharge moment, in the same transaction — a null
        // result just means billing is disabled for this clinic, which is not an error here.
        await sender.Send(new FinalizeIpdBillCommand(registration.Id, registration.ApplicationId, dischargeDate), cancellationToken);

        await repository.SaveChangesAsync(cancellationToken);

        return IpdRegistrationTranslator.ToDto(registration);
    }
}
