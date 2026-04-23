using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Applications.Commands;
using TenantCore.Application.Services;

namespace TenantCore.Application.Features.Applications.Handlers;

public sealed class ToggleUserApplicationMappingHandler(
    IAuthApplicationService authApplicationService,
    ILogger<ToggleUserApplicationMappingHandler> logger)
    : IRequestHandler<ToggleUserApplicationMappingCommand>
{
    public async Task Handle(ToggleUserApplicationMappingCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Toggling user {UserId} mapping for application {ApplicationId} to {IsActive}",
            request.UserId, request.ApplicationId, request.IsActive);
        await authApplicationService.ToggleUserApplicationMappingAsync(
            request.ApplicationId, request.UserId, request.ModifiedBy, request.IsActive, cancellationToken);
        logger.LogInformation("User {UserId} mapping for application {ApplicationId} toggled to {IsActive}",
            request.UserId, request.ApplicationId, request.IsActive);
    }
}
