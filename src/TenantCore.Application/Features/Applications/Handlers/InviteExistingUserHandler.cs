using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Applications.Commands;
using TenantCore.Application.Services;

namespace TenantCore.Application.Features.Applications.Handlers;

public sealed class InviteExistingUserHandler(
    IAuthApplicationService authApplicationService,
    ILogger<InviteExistingUserHandler> logger)
    : IRequestHandler<InviteExistingUserCommand>
{
    public async Task Handle(InviteExistingUserCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Inviting existing user {UserId} to application {ApplicationId}",
            request.Request.UserId, request.Request.ApplicationId);
        await authApplicationService.InviteExistingUserAsync(request.InvitedBy, request.Request, cancellationToken);
        logger.LogInformation("Existing user {UserId} invited to application {ApplicationId}",
            request.Request.UserId, request.Request.ApplicationId);
    }
}
