using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Applications.Commands;
using TenantCore.Application.Services;

namespace TenantCore.Application.Features.Applications.Handlers;

public sealed class ToggleApplicationStatusHandler(
    IAuthApplicationService authApplicationService,
    ILogger<ToggleApplicationStatusHandler> logger)
    : IRequestHandler<ToggleApplicationStatusCommand>
{
    public async Task Handle(ToggleApplicationStatusCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Toggling application {ApplicationId} status to {IsActive}", request.ApplicationId, request.IsActive);
        await authApplicationService.ToggleApplicationStatusAsync(request.ApplicationId, request.ModifiedBy, request.IsActive, cancellationToken);
        logger.LogInformation("Application {ApplicationId} status toggled to {IsActive}", request.ApplicationId, request.IsActive);
    }
}
