using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.VitalPresets.Translators;

public static class VitalPresetLookupItemTranslator
{
    public static VitalPresetLookupItemDto ToDto(VitalPresetLookupItem entity) => new()
    {
        Id = entity.Id,
        VitalField = entity.VitalField,
        Value = entity.Value,
        IsGlobal = entity.ApplicationId is null
    };

    public static IEnumerable<VitalPresetLookupItemDto> ToDtoList(IEnumerable<VitalPresetLookupItem> entities)
        => entities.Select(ToDto);
}
