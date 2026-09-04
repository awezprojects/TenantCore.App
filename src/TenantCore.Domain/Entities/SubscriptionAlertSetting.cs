using TenantCore.Domain.Common;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

/// <summary>
/// Global reminder-threshold configuration — NOT tenant-scoped, owned by the
/// future super-admin portal. Nothing in this repo currently sends the emails
/// described by these rows; that job is a separate Azure Function (see
/// PLAN.md "Out of Scope"). This table exists so the Function and the admin
/// portal have a schema to read and write today.
/// </summary>
public class SubscriptionAlertSetting : AuditableEntity
{
    public SubscriptionAlertType AlertType { get; private set; }
    public int DaysBeforeExpiry { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Headline { get; private set; } = string.Empty;
    public string BodyMessage { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; }
    public int DisplayOrder { get; private set; }

    private SubscriptionAlertSetting() { }

    /// <summary>Used only by SubscriptionAlertSettingConfiguration's HasData seed — fixed GUIDs, ValueGeneratedNever.</summary>
    public static SubscriptionAlertSetting CreateForSeed(
        Guid id,
        SubscriptionAlertType alertType,
        int daysBeforeExpiry,
        string subject,
        string headline,
        string bodyMessage,
        int displayOrder) => new()
        {
            Id = id,
            AlertType = alertType,
            DaysBeforeExpiry = daysBeforeExpiry,
            Subject = subject,
            Headline = headline,
            BodyMessage = bodyMessage,
            IsEnabled = true,
            DisplayOrder = displayOrder,
            CreatedAt = DateTime.UtcNow
        };

    public void Apply(string subject, string headline, string bodyMessage, bool isEnabled, int displayOrder)
    {
        Subject = subject;
        Headline = headline;
        BodyMessage = bodyMessage;
        IsEnabled = isEnabled;
        DisplayOrder = displayOrder;
        SetUpdatedAt();
    }
}
