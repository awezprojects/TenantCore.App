using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Applications.Queries;
using TenantCore.Application.Services;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Features.Applications.Handlers;

public sealed class GetApplicationInvitationsHandler(
    IAuthApplicationService authApplicationService,
    ILogger<GetApplicationInvitationsHandler> logger)
    : IRequestHandler<GetApplicationInvitationsQuery, List<InvitationResponseDto>>
{
    public async Task<List<InvitationResponseDto>> Handle(GetApplicationInvitationsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting invitations for application {ApplicationId}", request.ApplicationId);
        return await authApplicationService.GetApplicationInvitationsAsync(request.ApplicationId, cancellationToken);
    }
}
