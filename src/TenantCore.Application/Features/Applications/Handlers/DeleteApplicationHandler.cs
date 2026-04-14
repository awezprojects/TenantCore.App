using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Applications.Commands;
using TenantCore.Application.Services;

namespace TenantCore.Application.Features.Applications.Handlers;

public sealed class DeleteApplicationHandler(
    IAuthApplicationService authApplicationService,
    ILogger<DeleteApplicationHandler> logger)
    : IRequestHandler<DeleteApplicationCommand>
{
    public async Task Handle(DeleteApplicationCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting application {ApplicationId}", request.ApplicationId);
        await authApplicationService.DeleteApplicationAsync(request.ApplicationId, cancellationToken);
        logger.LogInformation("Application {ApplicationId} deleted", request.ApplicationId);
    }
}
