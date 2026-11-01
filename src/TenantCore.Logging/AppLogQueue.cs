using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TenantCore.Logging;

/// <summary>
/// Non-blocking front door for the log store. <see cref="QueuedAppLogWriter"/> enqueues and returns
/// immediately; <see cref="AppLogQueueService"/> drains into <see cref="AzureTableLogWriter"/>. Logging
/// is on for every command, request and outbound call, so it must never add a storage round-trip
/// to a request. When the queue is full the entry is dropped (a full queue means the store is
/// unreachable anyway) — logging never back-pressures or breaks the app.
/// </summary>
public sealed class AppLogQueue
{
    private readonly Channel<(string Table, LogEntry Entry)> _channel;

    public AppLogQueue(IOptions<AppLoggingOptions> options)
    {
        _channel = Channel.CreateBounded<(string, LogEntry)>(new BoundedChannelOptions(Math.Max(options.Value.QueueCapacity, 16))
        {
            // Wait (not DropWrite): TryWrite then returns false when full, so a drop is observable.
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true
        });
    }

    public bool TryEnqueue(string table, LogEntry entry) => _channel.Writer.TryWrite((table, entry));

    internal ChannelReader<(string Table, LogEntry Entry)> Reader => _channel.Reader;

    internal void Complete() => _channel.Writer.TryComplete();
}

/// <summary>The <see cref="IAppLogWriter"/> every caller gets: queue and return.</summary>
public sealed class QueuedAppLogWriter(AppLogQueue queue) : IAppLogWriter
{
    public Task WriteAsync(string tableName, LogEntry entry, CancellationToken ct = default)
    {
        queue.TryEnqueue(tableName, entry);
        return Task.CompletedTask;
    }
}

/// <summary>Drains <see cref="AppLogQueue"/> into Table Storage; every failure is swallowed.</summary>
public sealed class AppLogQueueService(
    AppLogQueue queue,
    AzureTableLogWriter writer,
    IOptions<AppLoggingOptions> options,
    ILogger<AppLogQueueService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ConnectionString))
        {
            logger.LogWarning("AppLogging:ConnectionString is not configured — logs are NOT being written to Table Storage.");
            await foreach (var _ in queue.Reader.ReadAllAsync(stoppingToken)) { }   // keep draining so memory stays bounded
            return;
        }

        try
        {
            await foreach (var (table, entry) in queue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await writer.WriteAsync(table, entry, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Debug only: an Error here would be copied back into this same failing store.
                    logger.LogDebug(ex, "Log write to table {Table} failed; entry dropped.", table);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Host shutting down.
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        queue.Complete();
        await base.StopAsync(cancellationToken);
    }
}
