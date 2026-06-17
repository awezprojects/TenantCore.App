using MediatR;

namespace TenantCore.Application.Features.Applications.Commands;

public sealed record ReinviteUserCommand(Guid ApplicationId, Guid InvitationId, Guid ReinvitedBy) : IRequest;
