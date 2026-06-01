using MediatR;

namespace TenantCore.Application.Features.Rooms.Commands;

public sealed record DeleteRoomCommand(Guid Id, Guid ApplicationId) : IRequest;
