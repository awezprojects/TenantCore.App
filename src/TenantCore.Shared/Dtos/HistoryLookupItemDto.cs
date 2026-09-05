using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos;

public class HistoryLookupItemDto
{
    public Guid Id { get; init; }
    public HistoryItemType Type { get; init; }
    public string Value { get; init; } = string.Empty;
    public bool IsGlobal { get; init; }
}

public sealed record AddHistoryLookupItemDto(HistoryItemType Type, string Value);
