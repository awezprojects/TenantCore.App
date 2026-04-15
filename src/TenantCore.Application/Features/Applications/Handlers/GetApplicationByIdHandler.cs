using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Applications.Queries;
using TenantCore.Application.Services;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Features.Applications.Handlers;

public sealed class GetApplicationByIdHandler(
    IAuthApplicationService authApplicationService,
    ILogger<GetApplicationByIdHandler> logger)
    : IRequestHandler<GetApplicationByIdQuery, ApplicationResponseDto?>
{
    public async Task<ApplicationResponseDto?> Handle(GetApplicationByIdQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting application {ApplicationId}", request.ApplicationId);
        return await authApplicationService.GetApplicationByIdAsync(request.ApplicationId, cancellationToken);
    }
}
