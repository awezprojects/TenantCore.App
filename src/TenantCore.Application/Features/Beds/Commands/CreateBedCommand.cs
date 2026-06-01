using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Beds.Commands;

public sealed record CreateBedCommand(Guid ApplicationId, Guid RoomId, string BedNumber) : IRequest<BedDto>;
