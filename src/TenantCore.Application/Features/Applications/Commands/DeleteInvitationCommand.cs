using MediatR;

namespace TenantCore.Application.Features.Applications.Commands;

public sealed record DeleteInvitationCommand(Guid ApplicationId, Guid InvitationId) : IRequest;
