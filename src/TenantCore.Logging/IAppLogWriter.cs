namespace TenantCore.Logging;

/// <summary>
/// Low-level contract: write one <see cref="LogEntry"/> to a named table/stream.
/// This is the single seam a future provider swap (Coralogix, Application Insights)
/// implements — everything upstream of it never changes.
/// </summary>
public interface IAppLogWriter
{
    Task WriteAsync(string tableName, LogEntry entry, CancellationToken ct = default);
}
