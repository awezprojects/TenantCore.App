using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Applications.Commands;
using TenantCore.Application.Services;

namespace TenantCore.Application.Features.Applications.Handlers;

public sealed class ChangeUserRoleHandler(
    IAuthApplicationService authApplicationService,
    ILogger<ChangeUserRoleHandler> logger)
    : IRequestHandler<ChangeUserRoleCommand>
{
    public async Task Handle(ChangeUserRoleCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Changing role for user {UserId} in application {ApplicationId} to role {NewRoleId}",
            request.UserId, request.ApplicationId, request.NewRoleId);
        await authApplicationService.ChangeUserRoleAsync(
            request.ApplicationId, request.UserId, request.ModifiedBy, request.NewRoleId, cancellationToken);
        logger.LogInformation("Role changed for user {UserId} in application {ApplicationId} to role {NewRoleId}",
            request.UserId, request.ApplicationId, request.NewRoleId);
    }
}
