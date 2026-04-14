using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Applications.Commands;
using TenantCore.Application.Services;

namespace TenantCore.Application.Features.Applications.Handlers;

public sealed class AssignUserHandler(
    IAuthApplicationService authApplicationService,
    ILogger<AssignUserHandler> logger)
    : IRequestHandler<AssignUserCommand>
{
    public async Task Handle(AssignUserCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Assigning user {UserId} to application {ApplicationId} with role {RoleId}", request.UserId, request.ApplicationId, request.RoleId);
        await authApplicationService.AssignUserToApplicationAsync(request.ApplicationId, request.UserId, request.RoleId, request.AssignedBy, cancellationToken);
        logger.LogInformation("User {UserId} assigned to application {ApplicationId}", request.UserId, request.ApplicationId);
    }
}
