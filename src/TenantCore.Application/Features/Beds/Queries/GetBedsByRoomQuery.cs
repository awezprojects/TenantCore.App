using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Beds.Queries;

public sealed record GetBedsByRoomQuery(Guid RoomId, Guid ApplicationId) : IRequest<IEnumerable<BedDto>>;
