using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Applications.Queries;
using TenantCore.Application.Services;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Applications.Handlers;

public sealed class GetDoctorsByApplicationHandler(
    IAuthApplicationService authApplicationService,
    ILogger<GetDoctorsByApplicationHandler> logger)
    : IRequestHandler<GetDoctorsByApplicationQuery, List<DoctorDto>>
{
    public async Task<List<DoctorDto>> Handle(GetDoctorsByApplicationQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting doctors for application {ApplicationId}", request.ApplicationId);
        var users = await authApplicationService.GetApplicationUsersAsync(request.ApplicationId, cancellationToken);
        return users
            .Where(u => u.RoleName == AppRoles.Doctor && u.IsActive)
            .Select(u => new DoctorDto { UserId = u.UserId, FullName = u.FullName, Email = u.EmailId })
            .ToList();
    }
}
