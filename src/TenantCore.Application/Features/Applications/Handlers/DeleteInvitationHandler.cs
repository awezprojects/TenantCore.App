using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Applications.Commands;
using TenantCore.Application.Services;

namespace TenantCore.Application.Features.Applications.Handlers;

public sealed class DeleteInvitationHandler(
    IAuthApplicationService authApplicationService,
    ILogger<DeleteInvitationHandler> logger)
    : IRequestHandler<DeleteInvitationCommand>
{
    public async Task Handle(DeleteInvitationCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting invitation {InvitationId} for application {ApplicationId}",
            request.InvitationId, request.ApplicationId);
        await authApplicationService.DeleteInvitationAsync(request.ApplicationId, request.InvitationId, cancellationToken);
    }
}
