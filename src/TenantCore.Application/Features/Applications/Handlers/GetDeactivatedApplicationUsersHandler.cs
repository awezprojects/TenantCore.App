using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Applications.Queries;
using TenantCore.Application.Services;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Features.Applications.Handlers;

public sealed class GetDeactivatedApplicationUsersHandler(
    IAuthApplicationService authApplicationService,
    ILogger<GetDeactivatedApplicationUsersHandler> logger)
    : IRequestHandler<GetDeactivatedApplicationUsersQuery, List<ApplicationUserResponseDto>>
{
    public async Task<List<ApplicationUserResponseDto>> Handle(GetDeactivatedApplicationUsersQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting deactivated users for application {ApplicationId}", request.ApplicationId);
        return await authApplicationService.GetDeactivatedApplicationUsersAsync(request.ApplicationId, cancellationToken);
    }
}
