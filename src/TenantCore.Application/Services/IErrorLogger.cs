using TenantCore.Shared.Enums;

namespace TenantCore.Application.Services;

/// <summary>
/// Captures the real exception detail (type, stack trace, context) that the user-facing
/// message pipeline deliberately hides. Available to any Application handler or
/// Infrastructure service — inject it like any other dependency. Implementations must
/// never throw: a logging failure must not break the operation being logged.
/// </summary>
public interface IErrorLogger
{
    Task LogAsync(
        LogCategory category,
        string source,
        string message,
        string? exceptionType = null,
        string? stackTrace = null,
        Guid? applicationId = null,
        string? userId = null,
        string? additionalContext = null,
        CancellationToken ct = default);
}

/// <summary>
/// Convenience overload for the common case of logging a real caught <see cref="Exception"/>
/// (middleware, repositories, external service calls) without every caller having to
/// unpack its type name and stack trace by hand.
/// </summary>
public static class ErrorLoggerExtensions
{
    public static Task LogExceptionAsync(
        this IErrorLogger logger,
        LogCategory category,
        string source,
        Exception exception,
        Guid? applicationId = null,
        string? userId = null,
        string? additionalContext = null,
        CancellationToken ct = default) =>
        logger.LogAsync(
            category,
            source,
            exception.Message,
            exception.GetType().FullName ?? exception.GetType().Name,
            exception.ToString(),
            applicationId,
            userId,
            additionalContext,
            ct);
}
