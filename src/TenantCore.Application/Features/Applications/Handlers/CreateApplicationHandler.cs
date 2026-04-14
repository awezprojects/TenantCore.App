using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Applications.Commands;
using TenantCore.Application.Services;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Features.Applications.Handlers;

public sealed class CreateApplicationHandler(
    IAuthApplicationService authApplicationService,
    ILogger<CreateApplicationHandler> logger)
    : IRequestHandler<CreateApplicationCommand, ApplicationResponseDto>
{
    public async Task<ApplicationResponseDto> Handle(CreateApplicationCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating application {ApplicationName} for owner {OwnerId}", request.Request.ApplicationName, request.OwnerId);
        var result = await authApplicationService.CreateApplicationAsync(request.OwnerId, request.Request, cancellationToken);
        logger.LogInformation("Application created with id {ApplicationId}", result.ApplicationId);
        return result;
    }
}
