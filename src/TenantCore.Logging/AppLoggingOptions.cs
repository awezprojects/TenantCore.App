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
}
