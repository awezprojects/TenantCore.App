using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos.Subscriptions;

/// <summary>
/// Read model of one reminder threshold row, for the future super-admin portal.
/// Nothing in TenantCore.App acts on these rows today — see PLAN.md.
/// </summary>
public record SubscriptionAlertSettingDto
{
    public Guid Id { get; init; }
    public SubscriptionAlertType AlertType { get; init; }
    public int DaysBeforeExpiry { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Headline { get; init; } = string.Empty;
    public string BodyMessage { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public int DisplayOrder { get; init; }
}
