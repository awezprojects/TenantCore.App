using TenantCore.Domain.Common;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

public class VitalPresetLookupItem : BaseEntity
{
    // null = system default, visible to every clinic; set = a clinic's own custom addition,
    // visible only to that clinic. Same pattern as HistoryLookupItem.
    public Guid? ApplicationId { get; private set; }
    public VitalFieldType VitalField { get; private set; }
    public string Value { get; private set; } = string.Empty;

    private VitalPresetLookupItem() { }

    public static VitalPresetLookupItem CreateGlobal(VitalFieldType vitalField, string value) => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = null,
        VitalField = vitalField,
        Value = value,
        CreatedAt = DateTime.UtcNow
    };

    public static VitalPresetLookupItem CreateForClinic(Guid applicationId, VitalFieldType vitalField, string value) => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = applicationId,
        VitalField = vitalField,
        Value = value,
        CreatedAt = DateTime.UtcNow
    };

    /// <summary>Used only by VitalPresetLookupItemConfiguration's HasData seed — fixed GUIDs, ValueGeneratedNever, fixed CreatedAt.</summary>
    public static VitalPresetLookupItem CreateForSeed(Guid id, VitalFieldType vitalField, string value, DateTime createdAt) => new()
    {
        Id = id,
        ApplicationId = null,
        VitalField = vitalField,
        Value = value,
        CreatedAt = createdAt
    };
}
