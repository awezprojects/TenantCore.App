using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Clinics.Queries;
using TenantCore.Application.Services;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Clinics.Handlers;

public sealed class GetClinicDoctorsHandler(
    IAuthApplicationService authService,
    ILogger<GetClinicDoctorsHandler> logger)
    : IRequestHandler<GetClinicDoctorsQuery, IEnumerable<DoctorDto>>
{
    public async Task<IEnumerable<DoctorDto>> Handle(
        GetClinicDoctorsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching doctors for application {ApplicationId}", request.ApplicationId);

        var users = await authService.GetApplicationUsersAsync(request.ApplicationId, cancellationToken);

        return users
            .Where(u => string.Equals(u.RoleName, AppRoles.Doctor, StringComparison.OrdinalIgnoreCase) && u.IsActive)
            .Select(u => new DoctorDto
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.EmailId
            });
    }
}
