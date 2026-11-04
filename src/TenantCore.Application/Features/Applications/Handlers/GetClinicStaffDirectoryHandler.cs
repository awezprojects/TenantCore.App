using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Applications.Queries;
using TenantCore.Application.Services;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Features.Applications.Handlers;

public sealed class GetClinicStaffDirectoryHandler(
    IAuthApplicationService authApplicationService,
    ILogger<GetClinicStaffDirectoryHandler> logger)
    : IRequestHandler<GetClinicStaffDirectoryQuery, List<ClinicStaffDirectoryDto>>
{
    public async Task<List<ClinicStaffDirectoryDto>> Handle(GetClinicStaffDirectoryQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting staff directory for application {ApplicationId}", request.ApplicationId);
        return await authApplicationService.GetClinicStaffDirectoryAsync(request.ApplicationId, request.Roles, cancellationToken);
    }
}
