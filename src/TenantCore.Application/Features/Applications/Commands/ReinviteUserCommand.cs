using MediatR;

namespace TenantCore.Application.Features.Applications.Commands;

public record ReinviteUserCommand(Guid ApplicationId, Guid InvitationId, Guid ReinvitedBy) : IRequest;
