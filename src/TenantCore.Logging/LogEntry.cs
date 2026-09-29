namespace TenantCore.Logging;

/// <summary>
/// The only shape this project knows about — a fully independent record of one error,
/// deliberately decoupled from any Domain/Application/Shared type so the writer behind
/// <see cref="IAppLogWriter"/> can be swapped (e.g. for Coralogix) without this project,
/// or any caller of it, changing shape.
/// </summary>
/// <remarks>
/// SHARED SCHEMA: TenantCore.Admin (TenantCore.Admin.Logging) and TenantCore.Auth write the same
/// columns, and the Admin Log Explorer reads all of them with one parser. Add a column in all three
/// (and to the explorer's parser) — never rename or retype one.
/// </remarks>
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

    /// <summary>
    /// Action rows: ties a "Started" row to its matching "Completed"/"Failed" row.
    /// Error/request rows: the HTTP request's correlation id (X-Correlation-Id / TraceIdentifier).
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>Action rows: "Started" / "Completed" / "Failed". Error rows written from ILogger: the log level.</summary>
    public string? Status { get; init; }

    /// <summary>Elapsed time, set on Completed/Failed action rows, request rows and outbound-call rows.</summary>
    public long? DurationMs { get; init; }

    /// <summary>HTTP method of the request (request/error rows) or of the outbound call (outbound rows).</summary>
    public string? HttpMethod { get; init; }

    /// <summary>The ACTUAL HTTP status returned (request/error rows) or received (outbound rows).</summary>
    public int? StatusCode { get; init; }
}

/// <summary>Category values written by this project's callers (the Category column).</summary>
public static class LogCategories
{
    public const string Api = "Api";
    public const string Frontend = "Frontend";
    public const string Action = "Action";
    public const string Request = "Request";
    public const string Outbound = "Outbound";
    public const string Server = "Server";
}
