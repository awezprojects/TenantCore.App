using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.Beds.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Authorization;

namespace TenantCore.Application.Features.Beds.Handlers;

public sealed class DeleteBedHandler(
    IBedRepository repository, IIpdBedTransferHistoryRepository historyRepository, IApplicationAccessValidator accessValidator)
    : IRequestHandler<DeleteBedCommand>
{
    public async Task Handle(DeleteBedCommand request, CancellationToken cancellationToken)
    {
        var bed = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Bed), request.Id);

        if (!accessValidator.CanAccess(bed.ApplicationId))
            throw new NotFoundException(nameof(Bed), request.Id);

        // SECURITY: the controller's [Authorize(Policy=RequireClinicAdmin)] resolves its clinic
        // from the X-Application-Id header for this route shape ({id:guid}, not {applicationId})
        // — see ClinicRoleAuthorizationHandler. A user who is Clinic Admin in one clinic and holds
        // any (even non-admin) role in another could otherwise select the admin clinic's header
        // and delete a bed belonging to the other clinic. CanAccess alone only proves linkage.
        if (!accessValidator.HasRole(bed.ApplicationId, [AppRoles.ClinicAdmin]))
            throw new NotFoundException(nameof(Bed), request.Id);

        if (bed.IsOccupied)
            throw new DomainValidationException("Cannot delete a bed that is currently occupied.");

        // DEF-IPD-17: IpdBedTransferHistory's FK to Bed is DeleteBehavior.Restrict — deleting a
        // bed with admission history used to fall straight through to a raw SqlException and an
        // unhandled 500. Caught here instead, with a 409 that points at the alternative that
        // already exists (Bed.Deactivate, via the existing activate/deactivate endpoint).
        if (await historyRepository.HasAnyForBedAsync(bed.Id, bed.ApplicationId, cancellationToken))
            throw new InvalidOperationException("This bed has admission history and cannot be deleted. Deactivate it instead.");

        repository.Delete(bed);
        await repository.SaveChangesAsync(cancellationToken);
    }
}
