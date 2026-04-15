using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Applications.Queries;
using TenantCore.Application.Services;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Features.Applications.Handlers;

public sealed class GetApplicationByCodeHandler(
    IAuthApplicationService authApplicationService,
    ILogger<GetApplicationByCodeHandler> logger)
    : IRequestHandler<GetApplicationByCodeQuery, ApplicationResponseDto?>
{
    public async Task<ApplicationResponseDto?> Handle(GetApplicationByCodeQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting application by code {Code}", request.Code);
        return await authApplicationService.GetApplicationByCodeAsync(request.Code, cancellationToken);
    }
}
