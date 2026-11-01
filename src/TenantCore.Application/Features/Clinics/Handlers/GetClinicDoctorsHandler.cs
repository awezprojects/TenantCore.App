using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Clinics.Queries;
using TenantCore.Application.Services;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Clinics.Handlers;

public sealed class GetClinicDoctorsHandler(
    IAuthApplicationService authService,
    IDoctorProfileRepository doctorProfileRepository,
    ILogger<GetClinicDoctorsHandler> logger)
    : IRequestHandler<GetClinicDoctorsQuery, IEnumerable<DoctorDto>>
{
    public async Task<IEnumerable<DoctorDto>> Handle(
        GetClinicDoctorsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching doctors for application {ApplicationId}", request.ApplicationId);

        // Minimal staff directory, not the Admin/Clinic Admin-only full roster — Reception must be
        // able to load this to populate the OPD doctor dropdown (DEF-AUTH-OPD-01). Unfiltered (no
        // ?roles=) to preserve the DoctorProfile fallback below for users without an explicit Doctor role.
        var staff = await authService.GetClinicStaffDirectoryAsync(request.ApplicationId, roles: null, cancellationToken);

        var allUserIds = staff.Select(u => u.UserId);
        var profiles = await doctorProfileRepository.GetByUserIdsAsync(allUserIds, cancellationToken);
        var profileByUserId = profiles.ToDictionary(p => p.UserId, p => p.Id);

        var doctorUsers = staff
            .Where(u => HasDoctorRole(u) || profileByUserId.ContainsKey(u.UserId))
            .ToList();

        return doctorUsers.Select(u => new DoctorDto
        {
            UserId = u.UserId,
            FullName = u.FullName,
            ProfileId = profileByUserId.GetValueOrDefault(u.UserId)
        });
    }

    private static bool HasDoctorRole(Shared.Dtos.Auth.ClinicStaffDirectoryDto user) =>
        user.RoleNames.Exists(r => string.Equals(r, AppRoles.Doctor, StringComparison.OrdinalIgnoreCase))
        || string.Equals(user.RoleName, AppRoles.Doctor, StringComparison.OrdinalIgnoreCase);
}
