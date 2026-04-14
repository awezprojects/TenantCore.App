using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Applications.Queries;
using TenantCore.Application.Services;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Features.Applications.Handlers;

public sealed class GetApplicationsByTypeHandler(
    IAuthApplicationService authApplicationService,
    ILogger<GetApplicationsByTypeHandler> logger)
    : IRequestHandler<GetApplicationsByTypeQuery, List<ApplicationResponseDto>>
{
    public async Task<List<ApplicationResponseDto>> Handle(GetApplicationsByTypeQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting applications by type {ApplicationType}", request.ApplicationType);
        return await authApplicationService.GetApplicationsByTypeAsync(request.ApplicationType, cancellationToken);
    }
}
