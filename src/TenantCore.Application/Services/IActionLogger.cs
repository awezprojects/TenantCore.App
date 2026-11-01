namespace TenantCore.Application.Services;

/// <summary>
/// Writes the business-action audit trail — a "Started" row and a matching "Completed"/"Failed"
/// row per command, tied together by <paramref name="correlationId"/>. <c>requestType</c> is the
/// request type name, optionally followed by " | " and identifier-only context (IActionLogContext).
/// Used by
/// <see cref="Common.Behaviors.ActionLoggingBehavior{TRequest,TResponse}"/>; implementations must
/// never throw — a logging failure must not break the operation being audited.
/// </summary>
public interface IActionLogger
{
    Task LogStartedAsync(
        Guid correlationId,
        string actionName,
        string requestType,
        Guid? applicationId,
        Guid? userId,
        CancellationToken ct = default);

    Task LogCompletedAsync(
        Guid correlationId,
        string actionName,
        string requestType,
        Guid? applicationId,
        Guid? userId,
        long durationMs,
        CancellationToken ct = default);

    Task LogFailedAsync(
        Guid correlationId,
        string actionName,
        string requestType,
        Guid? applicationId,
        Guid? userId,
        long durationMs,
        string errorMessage,
        CancellationToken ct = default);
}
