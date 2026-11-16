using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.Wards.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Authorization;

namespace TenantCore.Application.Features.Wards.Handlers;

public sealed class DeleteWardHandler(
    IWardRepository repository, IIpdBedTransferHistoryRepository historyRepository, IApplicationAccessValidator accessValidator)
    : IRequestHandler<DeleteWardCommand>
{
    public async Task Handle(DeleteWardCommand request, CancellationToken cancellationToken)
    {
        var ward = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Ward), request.Id);

        if (!accessValidator.CanAccess(ward.ApplicationId))
            throw new NotFoundException(nameof(Ward), request.Id);

        // SECURITY: see DeleteBedHandler for why CanAccess alone is not enough for this route
        // shape — this re-checks the role specifically against ward.ApplicationId.
        if (!accessValidator.HasRole(ward.ApplicationId, [AppRoles.ClinicAdmin]))
            throw new NotFoundException(nameof(Ward), request.Id);

        // DEF-IPD-17: same raw-500-on-FK-violation problem as beds/rooms, one level up.
        // GetByIdAsync already includes Rooms.Beds (WardRepository), so the occupancy check is
        // free; the history check needs one more query. Deactivate is the alternative.
        if (ward.Rooms.Any(r => r.Beds.Any(b => b.IsOccupied)))
            throw new DomainValidationException("Cannot delete a ward with an occupied bed.");

        if (await historyRepository.HasAnyForWardAsync(ward.Id, ward.ApplicationId, cancellationToken))
            throw new InvalidOperationException("This ward has admission history and cannot be deleted. Deactivate it instead.");

        repository.Delete(ward);
        await repository.SaveChangesAsync(cancellationToken);
    }
}
