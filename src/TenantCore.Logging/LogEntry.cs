namespace TenantCore.Logging;

/// <summary>
/// The only shape this project knows about — a fully independent record of one error,
/// deliberately decoupled from any Domain/Application/Shared type so the writer behind
/// <see cref="IAppLogWriter"/> can be swapped (e.g. for Coralogix) without this project,
/// or any caller of it, changing shape.
/// </summary>
public sealed class LogEntry
{
    public required string Category { get; init; }
    public required string Source { get; init; }
    public required string Message { get; init; }
    public string? ExceptionType { get; init; }
    public string? StackTrace { get; init; }
    public string? ApplicationId { get; init; }
    public string? UserId { get; init; }
    public string? RequestPath { get; init; }
    public string? AdditionalContext { get; init; }
    public required string Environment { get; init; }
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
