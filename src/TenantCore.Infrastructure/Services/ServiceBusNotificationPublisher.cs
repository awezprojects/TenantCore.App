using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Services;

namespace TenantCore.Infrastructure.Services;

/// <summary>
/// Mirrors TenantCore.Auth's ServiceBusNotificationPublisher: publishes to the same
/// "AzureServiceBus:ConnectionString" / "AzureServiceBus:QueueName" queue instead of sending
/// email directly. A separate notification consumer drains the queue and does the actual send.
/// Singleton — ServiceBusClient is thread-safe and meant to be long-lived (same lifetime Auth
/// uses it at).
/// </summary>
public class ServiceBusNotificationPublisher : INotificationPublisher, IAsyncDisposable
{
    private readonly ServiceBusClient? _client;
    private readonly ServiceBusSender? _sender;
    private readonly ILogger<ServiceBusNotificationPublisher> _logger;

    public ServiceBusNotificationPublisher(IConfiguration configuration, ILogger<ServiceBusNotificationPublisher> logger)
    {
        _logger = logger;

        var connectionString = configuration["AzureServiceBus:ConnectionString"];
        var queueName = configuration["AzureServiceBus:QueueName"];

        if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(queueName))
        {
            _logger.LogWarning("AzureServiceBus is not configured. Email notifications will not be published.");
            return;
        }

        _client = new ServiceBusClient(connectionString);
        _sender = _client.CreateSender(queueName);
    }

    public async Task PublishEmailNotificationAsync(EmailNotificationDto notification, CancellationToken ct = default)
    {
        if (_sender is null)
            throw new InvalidOperationException(
                "AzureServiceBus is not configured (AzureServiceBus:ConnectionString / AzureServiceBus:QueueName missing in configuration).");

        var json = JsonSerializer.Serialize(notification);
        var message = new ServiceBusMessage(json);

        await _sender.SendMessageAsync(message, ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_sender is not null)
            await _sender.DisposeAsync();
        if (_client is not null)
            await _client.DisposeAsync();
    }
}
