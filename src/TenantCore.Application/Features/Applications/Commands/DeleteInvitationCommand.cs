using MediatR;

namespace TenantCore.Application.Features.Applications.Commands;

public record DeleteInvitationCommand(Guid ApplicationId, Guid InvitationId) : IRequest;
