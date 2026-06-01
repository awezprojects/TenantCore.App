using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Rooms.Commands;

public sealed record CreateRoomCommand(Guid ApplicationId, Guid WardId, string RoomNumber, string? RoomType, decimal PricePerDay) : IRequest<RoomDto>;
