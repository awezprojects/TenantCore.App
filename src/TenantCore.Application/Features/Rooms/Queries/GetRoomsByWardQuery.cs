using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Rooms.Queries;

public sealed record GetRoomsByWardQuery(Guid WardId, Guid ApplicationId) : IRequest<IEnumerable<RoomDto>>;
