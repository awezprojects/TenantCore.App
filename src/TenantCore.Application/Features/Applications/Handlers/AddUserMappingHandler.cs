using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Applications.Commands;
using TenantCore.Application.Services;

namespace TenantCore.Application.Features.Applications.Handlers;

public sealed class AddUserMappingHandler(
    IAuthApplicationService authApplicationService,
    ILogger<AddUserMappingHandler> logger)
    : IRequestHandler<AddUserMappingCommand>
{
    public async Task Handle(AddUserMappingCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Adding user {UserId} mapping to application {ApplicationId}", request.UserId, request.ApplicationId);
        await authApplicationService.AddApplicationUserMappingAsync(request.ApplicationId, request.UserId, request.AssignedBy, cancellationToken);
        logger.LogInformation("User {UserId} mapped to application {ApplicationId}", request.UserId, request.ApplicationId);
    }
}
