using MediatR;
using TenantCore.Application.Common;

namespace TenantCore.Application.Features.Applications.Commands;

public sealed record DeleteInvitationCommand(Guid ApplicationId, Guid InvitationId) : IRequest, IBusinessAction
{
    public string ActionName => "Invitation Deletion";
}
