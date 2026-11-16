using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.Rooms.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Authorization;

namespace TenantCore.Application.Features.Rooms.Handlers;

public sealed class DeleteRoomHandler(
    IRoomRepository repository, IIpdBedTransferHistoryRepository historyRepository, IApplicationAccessValidator accessValidator)
    : IRequestHandler<DeleteRoomCommand>
{
    public async Task Handle(DeleteRoomCommand request, CancellationToken cancellationToken)
    {
        var room = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Room), request.Id);

        if (!accessValidator.CanAccess(room.ApplicationId))
            throw new NotFoundException(nameof(Room), request.Id);

        // SECURITY: see DeleteBedHandler for why CanAccess alone is not enough for this route
        // shape — this re-checks the role specifically against room.ApplicationId.
        if (!accessValidator.HasRole(room.ApplicationId, [AppRoles.ClinicAdmin]))
            throw new NotFoundException(nameof(Room), request.Id);

        // DEF-IPD-17: these used to fall straight through to the DB's FK constraints (an occupied
        // bed's Bed.ApplicationId FK, or IpdBedTransferHistory's Restrict FK to Room) as a raw,
        // unhandled 500. GetByIdAsync already includes Beds (RoomRepository), so the occupancy
        // check is free; the history check needs one more query. Deactivate is the alternative.
        if (room.Beds.Any(b => b.IsOccupied))
            throw new DomainValidationException("Cannot delete a room with an occupied bed.");

        if (await historyRepository.HasAnyForRoomAsync(room.Id, room.ApplicationId, cancellationToken))
            throw new InvalidOperationException("This room has admission history and cannot be deleted. Deactivate it instead.");

        repository.Delete(room);
        await repository.SaveChangesAsync(cancellationToken);
    }
}
