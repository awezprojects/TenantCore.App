using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Applications.Commands;
using TenantCore.Application.Services;

namespace TenantCore.Application.Features.Applications.Handlers;

public sealed class RemoveUserHandler(
    IAuthApplicationService authApplicationService,
    ILogger<RemoveUserHandler> logger)
    : IRequestHandler<RemoveUserCommand>
{
    public async Task Handle(RemoveUserCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Removing user {UserId} from application {ApplicationId}", request.UserId, request.ApplicationId);
        await authApplicationService.RemoveUserFromApplicationAsync(request.ApplicationId, request.UserId, request.RemovedBy, cancellationToken);
        logger.LogInformation("User {UserId} removed from application {ApplicationId}", request.UserId, request.ApplicationId);
    }
}
