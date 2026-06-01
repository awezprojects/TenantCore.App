using MediatR;
using TenantCore.Application.Features.Rooms.Commands;
using TenantCore.Application.Features.Rooms.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Rooms.Handlers;

public sealed class UpdateRoomHandler(IRoomRepository repository) : IRequestHandler<UpdateRoomCommand, RoomDto>
{
    public async Task<RoomDto> Handle(UpdateRoomCommand request, CancellationToken cancellationToken)
    {
        var room = await repository.GetByIdWithBedsAsync(request.Id, request.ApplicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Room), request.Id);

        if (await repository.ExistsByNumberAsync(room.WardId, request.RoomNumber, excludeId: request.Id, cancellationToken: cancellationToken))
            throw new InvalidOperationException(TenantCore.Shared.Errors.UserMessages.RoomNumberTaken);

        room.Update(request.RoomNumber, request.RoomType, request.PricePerDay);
        repository.Update(room);
        await repository.SaveChangesAsync(cancellationToken);
        return RoomTranslator.ToDto(room);
    }
}
