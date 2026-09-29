namespace TenantCore.Logging;

/// <summary>
/// Bound from the "AppLogging" configuration section. Changing <see cref="Provider"/>
/// (once more providers exist) is the only step needed to swap the backing store.
/// </summary>
public sealed class AppLoggingOptions
{
    public const string SectionName = "AppLogging";

    /// <summary>Reserved for a future provider switch (e.g. "TableStorage", "Coralogix"). Only "TableStorage" is implemented today.</summary>
    public string Provider { get; set; } = "TableStorage";

    public string ConnectionString { get; set; } = string.Empty;
    public string ApiErrorTable { get; set; } = "ApiErrorLogs";
    public string FrontendErrorTable { get; set; } = "FrontendErrorLogs";
    public string ActionLogTable { get; set; } = "ActionLogs";

    /// <summary>One row per API request that failed (≥400), was slow, or changed data — see the Request* options.</summary>
    public string RequestLogTable { get; set; } = "ApiRequestLogs";

    /// <summary>Entries buffered in memory before new ones are dropped (logging never back-pressures a request).</summary>
    public int QueueCapacity { get; set; } = 5000;

    /// <summary>Lowest ILogger/Serilog level also copied to ApiErrorLogs (background jobs, hosted services…): Warning, Error or Fatal.</summary>
    public string MinimumLevel { get; set; } = "Error";

    /// <summary>A request at or above this duration is logged even when it succeeded.</summary>
    public int SlowRequestMs { get; set; } = 2000;

    /// <summary>Log every successful POST/PUT/PATCH/DELETE request (reads are logged only when failed or slow).</summary>
    public bool LogMutatingRequests { get; set; } = true;
}
