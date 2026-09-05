using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.HistoryLookupItems.Translators;

public static class HistoryLookupItemTranslator
{
    public static HistoryLookupItemDto ToDto(HistoryLookupItem entity) => new()
    {
        Id = entity.Id,
        Type = entity.Type,
        Value = entity.Value,
        IsGlobal = entity.ApplicationId is null
    };

    public static IEnumerable<HistoryLookupItemDto> ToDtoList(IEnumerable<HistoryLookupItem> entities)
        => entities.Select(ToDto);
}
