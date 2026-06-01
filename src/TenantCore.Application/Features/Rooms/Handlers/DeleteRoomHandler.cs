using MediatR;
using TenantCore.Application.Features.Rooms.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.Rooms.Handlers;

public sealed class DeleteRoomHandler(IRoomRepository repository) : IRequestHandler<DeleteRoomCommand>
{
    public async Task Handle(DeleteRoomCommand request, CancellationToken cancellationToken)
    {
        var room = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Room), request.Id);

        if (room.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(Room), request.Id);

        repository.Delete(room);
        await repository.SaveChangesAsync(cancellationToken);
    }
}
