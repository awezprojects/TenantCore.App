using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Applications.Commands;
using TenantCore.Application.Services;

namespace TenantCore.Application.Features.Applications.Handlers;

public sealed class ReinviteUserHandler(
    IAuthApplicationService authApplicationService,
    ILogger<ReinviteUserHandler> logger)
    : IRequestHandler<ReinviteUserCommand>
{
    public async Task Handle(ReinviteUserCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Reinviting invitation {InvitationId} for application {ApplicationId}", request.InvitationId, request.ApplicationId);
        await authApplicationService.ReinviteUserAsync(request.ApplicationId, request.InvitationId, request.ReinvitedBy, cancellationToken);
    }
}
