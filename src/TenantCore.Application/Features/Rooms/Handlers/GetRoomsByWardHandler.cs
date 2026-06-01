using MediatR;
using TenantCore.Application.Features.Rooms.Queries;
using TenantCore.Application.Features.Rooms.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Rooms.Handlers;

public sealed class GetRoomsByWardHandler(IRoomRepository repository) : IRequestHandler<GetRoomsByWardQuery, IEnumerable<RoomDto>>
{
    public async Task<IEnumerable<RoomDto>> Handle(GetRoomsByWardQuery request, CancellationToken cancellationToken)
    {
        var rooms = await repository.GetByWardAsync(request.WardId, request.ApplicationId, cancellationToken);
        return rooms.Select(RoomTranslator.ToDto);
    }
}
