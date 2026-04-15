using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Applications.Queries;
using TenantCore.Application.Services;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Features.Applications.Handlers;

public sealed class GetApplicationUsersHandler(
    IAuthApplicationService authApplicationService,
    ILogger<GetApplicationUsersHandler> logger)
    : IRequestHandler<GetApplicationUsersQuery, List<ApplicationUserResponseDto>>
{
    public async Task<List<ApplicationUserResponseDto>> Handle(GetApplicationUsersQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting users for application {ApplicationId}", request.ApplicationId);
        return await authApplicationService.GetApplicationUsersAsync(request.ApplicationId, cancellationToken);
    }
}
