using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TenantCore.Application.Services;
using TenantCore.Logging;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Services;

/// <summary>
/// Adapter boundary: Application never sees <see cref="TenantCore.Logging"/> types directly.
/// Builds a <see cref="LogEntry"/> for each Started/Completed/Failed call and delegates to
/// <see cref="IAppLogWriter"/> against the <c>ActionLogs</c> table. Never throws — a logging
/// failure must not break the operation being audited.
/// </summary>
public sealed class ActionLoggingService(
    IAppLogWriter logWriter,
    IOptions<AppLoggingOptions> options,
    IHostEnvironment environment,
    ILogger<ActionLoggingService> logger)
    : IActionLogger
{
    public Task LogStartedAsync(
        Guid correlationId,
        string actionName,
        string requestType,
        Guid? applicationId,
        Guid? userId,
        CancellationToken ct = default) =>
        WriteAsync(correlationId, "Started", actionName, requestType, applicationId, userId, null, null, ct);

    public Task LogCompletedAsync(
        Guid correlationId,
        string actionName,
        string requestType,
        Guid? applicationId,
        Guid? userId,
        long durationMs,
        CancellationToken ct = default) =>
        WriteAsync(correlationId, "Completed", actionName, requestType, applicationId, userId, durationMs, null, ct);

    public Task LogFailedAsync(
        Guid correlationId,
        string actionName,
        string requestType,
        Guid? applicationId,
        Guid? userId,
        long durationMs,
        string errorMessage,
        CancellationToken ct = default) =>
        WriteAsync(correlationId, "Failed", actionName, requestType, applicationId, userId, durationMs, errorMessage, ct);

    private async Task WriteAsync(
        Guid correlationId,
        string status,
        string actionName,
        string requestType,
        Guid? applicationId,
        Guid? userId,
        long? durationMs,
        string? errorMessage,
        CancellationToken ct)
    {
        try
        {
            var entry = new LogEntry
            {
                Category = LogCategory.Action.ToString(),
                Source = actionName,
                Message = errorMessage ?? $"{actionName} {status.ToLowerInvariant()}",
                ApplicationId = applicationId is null || applicationId == Guid.Empty ? null : applicationId.ToString(),
                UserId = userId?.ToString(),
                AdditionalContext = requestType,
                Environment = environment.EnvironmentName,
                CorrelationId = correlationId.ToString(),
                Status = status,
                DurationMs = durationMs
            };

            await logWriter.WriteAsync(options.Value.ActionLogTable, entry, ct);
        }
        catch (Exception ex)
        {
            // Logging must never break the caller's operation.
            logger.LogWarning(ex, "Failed to write action log entry (Action={ActionName}, Status={Status})", actionName, status);
        }
    }
}
