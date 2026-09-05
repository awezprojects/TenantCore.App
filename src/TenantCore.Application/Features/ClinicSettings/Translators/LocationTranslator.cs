using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ClinicSettings.Translators;

public static class LocationTranslator
{
    public static StateDto ToDto(State entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Code = entity.Code
    };

    public static CityDto ToDto(City entity) => new()
    {
        Id = entity.Id,
        StateId = entity.StateId,
        Name = entity.Name
    };

    public static ClinicLocationDto ToDto(ClinicLocation entity) => new()
    {
        ApplicationId = entity.ApplicationId,
        StateId = entity.StateId,
        StateName = entity.State.Name,
        CityId = entity.CityId,
        CityName = entity.City.Name
    };
}
