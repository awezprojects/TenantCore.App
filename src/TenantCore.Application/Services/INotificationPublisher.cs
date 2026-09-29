namespace TenantCore.Application.Services;

/// <summary>
/// Publishes an email notification to the shared Azure Service Bus queue that TenantCore.Auth
/// also publishes to ("AzureServiceBus:QueueName", same connection string as Auth). Neither
/// service sends email directly — a separate notification consumer drains the queue and does
/// the actual delivery, keyed by <see cref="EmailNotificationDto.TemplateId"/>.
/// </summary>
public interface INotificationPublisher
{
    Task PublishEmailNotificationAsync(EmailNotificationDto notification, CancellationToken ct = default);
}

/// <summary>
/// Wire-compatible with TenantCore.Auth's EmailNotificationDto (same property names/shape) so
/// the same downstream notification consumer can read messages from either service.
/// </summary>
public class EmailNotificationDto
{
    public string RecipientEmail { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string TemplateId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public Dictionary<string, string> TemplateData { get; set; } = new();
}
