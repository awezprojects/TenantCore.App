using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Applications.Queries;
using TenantCore.Application.Services;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Features.Applications.Handlers;

public sealed class GetAllApplicationsHandler(
    IAuthApplicationService authApplicationService,
    ILogger<GetAllApplicationsHandler> logger)
    : IRequestHandler<GetAllApplicationsQuery, List<ApplicationResponseDto>>
{
    public async Task<List<ApplicationResponseDto>> Handle(GetAllApplicationsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting all applications");
        return await authApplicationService.GetAllApplicationsAsync(cancellationToken);
    }
}
