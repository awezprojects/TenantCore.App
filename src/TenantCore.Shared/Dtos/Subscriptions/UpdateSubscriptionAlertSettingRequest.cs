namespace TenantCore.Shared.Dtos.Subscriptions;

/// <summary>PUT body — edits one reminder threshold row.</summary>
public record UpdateSubscriptionAlertSettingRequest
{
    public string Subject { get; init; } = string.Empty;
    public string Headline { get; init; } = string.Empty;
    public string BodyMessage { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public int DisplayOrder { get; init; }
}
