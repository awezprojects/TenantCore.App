using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Applications.Commands;
using TenantCore.Application.Services;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Features.Applications.Handlers;

public sealed class EditApplicationHandler(
    IAuthApplicationService authApplicationService,
    ILogger<EditApplicationHandler> logger)
    : IRequestHandler<EditApplicationCommand, ApplicationResponseDto>
{
    public async Task<ApplicationResponseDto> Handle(EditApplicationCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Editing application {ApplicationId}", request.ApplicationId);
        var result = await authApplicationService.EditApplicationAsync(request.ApplicationId, request.Request, cancellationToken);
        logger.LogInformation("Application {ApplicationId} updated", request.ApplicationId);
        return result;
    }
}
