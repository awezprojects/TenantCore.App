using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.DoctorSpecialities.Translators;

public static class DoctorSpecialityTranslator
{
    public static DoctorSpecialityDto ToDto(DoctorSpeciality entity) => new()
    {
        Id          = entity.Id,
        Name        = entity.Name,
        Description = entity.Description,
        SortOrder   = entity.SortOrder,
    };

    public static List<DoctorSpecialityDto> ToDtoList(IEnumerable<DoctorSpeciality> entities)
        => entities.Select(ToDto).ToList();
}
