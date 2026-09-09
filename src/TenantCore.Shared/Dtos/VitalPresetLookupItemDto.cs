using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos;

public class VitalPresetLookupItemDto
{
    public Guid Id { get; init; }
    public VitalFieldType VitalField { get; init; }
    public string Value { get; init; } = string.Empty;
    public bool IsGlobal { get; init; }
}

public sealed record AddVitalPresetLookupItemDto(VitalFieldType VitalField, string Value);
