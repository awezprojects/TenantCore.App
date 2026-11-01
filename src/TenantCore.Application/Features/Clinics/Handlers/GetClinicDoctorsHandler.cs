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
        // able to load this to populate the OPD doctor dropdown (DEF-AUTH-OPD-01).
        var doctorUsers = await authService.GetClinicStaffDirectoryAsync(request.ApplicationId, [AppRoles.Doctor], cancellationToken);

        var doctorUserIds = doctorUsers.Select(u => u.UserId);
        var profiles = await doctorProfileRepository.GetByUserIdsAsync(doctorUserIds, cancellationToken);
        var profileByUserId = profiles.ToDictionary(p => p.UserId, p => p.Id);

        return doctorUsers.Select(u => new DoctorDto
        {
            UserId = u.UserId,
            FullName = u.FullName,
            ProfileId = profileByUserId.GetValueOrDefault(u.UserId)
        });
    }
}
