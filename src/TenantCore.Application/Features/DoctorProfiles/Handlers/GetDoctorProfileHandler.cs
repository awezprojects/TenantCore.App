using MediatR;
using TenantCore.Application.Features.DoctorProfiles.Queries;
using TenantCore.Application.Features.DoctorProfiles.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.DoctorProfiles.Handlers;

public sealed class GetDoctorProfileHandler(IDoctorProfileRepository repository)
    : IRequestHandler<GetDoctorProfileQuery, DoctorProfileDto?>
{
    public async Task<DoctorProfileDto?> Handle(GetDoctorProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await repository.GetByUserIdAsync(request.UserId, cancellationToken);
        return profile is null ? null : DoctorProfileTranslator.ToDto(profile);
    }
}
