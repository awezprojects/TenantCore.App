using MediatR;
using TenantCore.Application.Common;

namespace TenantCore.Application.Features.Rooms.Commands;

public sealed record DeleteRoomCommand(Guid Id, Guid ApplicationId) : IRequest, IBusinessAction
{
    public string ActionName => "Room Deletion";
}
