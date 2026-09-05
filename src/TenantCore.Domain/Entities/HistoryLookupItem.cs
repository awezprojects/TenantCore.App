using TenantCore.Domain.Common;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

public class HistoryLookupItem : BaseEntity
{
    // null = system default, visible to every clinic; set = a clinic's own custom addition,
    // visible only to that clinic.
    public Guid? ApplicationId { get; private set; }
    public HistoryItemType Type { get; private set; }
    public string Value { get; private set; } = string.Empty;

    private HistoryLookupItem() { }

    public static HistoryLookupItem CreateGlobal(HistoryItemType type, string value) => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = null,
        Type = type,
        Value = value,
        CreatedAt = DateTime.UtcNow
    };

    public static HistoryLookupItem CreateForClinic(Guid applicationId, HistoryItemType type, string value) => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = applicationId,
        Type = type,
        Value = value,
        CreatedAt = DateTime.UtcNow
    };
}
