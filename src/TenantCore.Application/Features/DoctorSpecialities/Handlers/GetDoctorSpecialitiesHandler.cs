using MediatR;
using TenantCore.Application.Features.DoctorSpecialities.Queries;
using TenantCore.Application.Features.DoctorSpecialities.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.DoctorSpecialities.Handlers;

public sealed class GetDoctorSpecialitiesHandler(IDoctorSpecialityRepository repository)
    : IRequestHandler<GetDoctorSpecialitiesQuery, List<DoctorSpecialityDto>>
{
    public async Task<List<DoctorSpecialityDto>> Handle(
        GetDoctorSpecialitiesQuery request, CancellationToken cancellationToken)
    {
        var specialities = await repository.GetAllActiveAsync(cancellationToken);
        return DoctorSpecialityTranslator.ToDtoList(specialities);
    }
}
