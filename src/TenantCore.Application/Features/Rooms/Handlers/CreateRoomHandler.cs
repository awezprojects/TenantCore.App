using MediatR;
using TenantCore.Application.Features.Rooms.Commands;
using TenantCore.Application.Features.Rooms.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Errors;

namespace TenantCore.Application.Features.Rooms.Handlers;

public sealed class CreateRoomHandler(IRoomRepository roomRepository, IWardRepository wardRepository)
    : IRequestHandler<CreateRoomCommand, RoomDto>
{
    public async Task<RoomDto> Handle(CreateRoomCommand request, CancellationToken cancellationToken)
    {
        var ward = await wardRepository.GetByIdAsync(request.WardId, cancellationToken)
            ?? throw new NotFoundException(nameof(Ward), request.WardId);

        if (ward.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(Ward), request.WardId);

        if (await roomRepository.ExistsByNumberAsync(request.WardId, request.RoomNumber, cancellationToken: cancellationToken))
            throw new InvalidOperationException(UserMessages.RoomNumberTaken);

        var room = Room.Create(request.ApplicationId, request.WardId, request.RoomNumber, request.RoomType, request.PricePerDay);
        await roomRepository.AddAsync(room, cancellationToken);
        await roomRepository.SaveChangesAsync(cancellationToken);

        var loaded = await roomRepository.GetByIdWithBedsAsync(room.Id, request.ApplicationId, cancellationToken) ?? room;
        return RoomTranslator.ToDto(loaded);
    }
}
