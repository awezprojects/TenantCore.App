using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Options;

namespace TenantCore.Logging;

/// <summary>
/// Current implementation of <see cref="IAppLogWriter"/> — writes to Azure Table Storage.
/// Creates the target table on first use. Callers are expected to swallow failures from
/// this writer (logging must never break the app it's instrumenting).
/// </summary>
public sealed class AzureTableLogWriter : IAppLogWriter
{
    private readonly IOptions<AppLoggingOptions> _options;
    private TableServiceClient? _serviceClient;

    public AzureTableLogWriter(IOptions<AppLoggingOptions> options)
    {
        _options = options;
    }

    // Deliberately lazy: this type is Singleton and gets resolved on every request via
    // ExceptionHandlingMiddleware's IErrorLogger parameter, not just when an error occurs.
    // Throwing here at DI-activation time (e.g. from the constructor) would take down every
    // request in the app whenever AppLogging:ConnectionString isn't configured yet. Failing
    // here instead means the throw only happens inside WriteAsync, where ErrorLoggingService's
    // catch already swallows it — exactly the "must never break the caller" contract.
    private TableServiceClient GetServiceClient()
    {
        if (_serviceClient is not null) return _serviceClient;

        var connectionString = _options.Value.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("AppLogging:ConnectionString is not configured.");

        return _serviceClient = new TableServiceClient(connectionString);
    }

    public async Task WriteAsync(string tableName, LogEntry entry, CancellationToken ct = default)
    {
        var tableClient = GetServiceClient().GetTableClient(tableName);
        await tableClient.CreateIfNotExistsAsync(ct);

        var tableEntity = new TableEntity(
            partitionKey: entry.TimestampUtc.ToString("yyyy-MM-dd"),
            rowKey: $"{DateTime.MaxValue.Ticks - entry.TimestampUtc.Ticks:d19}-{Guid.NewGuid():N}")
        {
            { nameof(LogEntry.Category), entry.Category },
            { nameof(LogEntry.Source), entry.Source },
            { nameof(LogEntry.Message), entry.Message },
            { nameof(LogEntry.ExceptionType), entry.ExceptionType },
            { nameof(LogEntry.StackTrace), entry.StackTrace },
            { nameof(LogEntry.ApplicationId), entry.ApplicationId },
            { nameof(LogEntry.UserId), entry.UserId },
            { nameof(LogEntry.RequestPath), entry.RequestPath },
            { nameof(LogEntry.AdditionalContext), entry.AdditionalContext },
            { nameof(LogEntry.Environment), entry.Environment },
            { nameof(LogEntry.TimestampUtc), entry.TimestampUtc }
        };

        await tableClient.AddEntityAsync(tableEntity, ct);
    }
}
