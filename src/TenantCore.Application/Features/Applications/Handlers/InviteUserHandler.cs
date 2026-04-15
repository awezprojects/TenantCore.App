using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Applications.Commands;
using TenantCore.Application.Services;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Features.Applications.Handlers;

public sealed class InviteUserHandler(
    IAuthApplicationService authApplicationService,
    ILogger<InviteUserHandler> logger)
    : IRequestHandler<InviteUserCommand, InvitationResponseDto>
{
    public async Task<InvitationResponseDto> Handle(InviteUserCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Inviting user {Email} to application {ApplicationId}", request.Request.EmailId, request.Request.ApplicationId);
        var result = await authApplicationService.InviteUserAsync(request.InvitedBy, request.Request, cancellationToken);
        logger.LogInformation("Invitation sent to {Email}", request.Request.EmailId);
        return result;
    }
}
