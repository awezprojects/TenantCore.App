using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Rooms.Commands;

public sealed record UpdateRoomCommand(Guid Id, Guid ApplicationId, string RoomNumber, string? RoomType, decimal PricePerDay) : IRequest<RoomDto>;
