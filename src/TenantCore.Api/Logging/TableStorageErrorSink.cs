using System.Security.Claims;
using Microsoft.Extensions.Options;
using Serilog.Core;
using Serilog.Events;
using TenantCore.Logging;

namespace TenantCore.Api.Logging;

/// <summary>
/// Serilog sink (picked up from DI by <c>ReadFrom.Services</c>) that copies Serilog/ILogger events at
/// or above AppLogging:MinimumLevel (default Error) into ApiErrorLogs — so failures in background
/// jobs, hosted services, EF, startup and anything else that only calls <c>logger.LogError</c> reach
/// the log store, not just the console (ADR-011). Queued (non-blocking) and scrubbed by the writer.
/// Skips sources whose errors are already written with richer context, and the logging pipeline's
/// own diagnostics (so a failing store can never feed itself).
/// </summary>
public sealed class TableStorageErrorSink(
    IAppLogWriter logWriter,
    IOptions<AppLoggingOptions> options,
    IHostEnvironment environment,
    IHttpContextAccessor httpContextAccessor) : ILogEventSink
{
    private static readonly string[] SkippedSources =
    [
        "TenantCore.Api.Middleware.ExceptionHandlingMiddleware",   // already in ApiErrorLogs via IErrorLogger (with tenant/user)
        "Serilog.AspNetCore.RequestLoggingMiddleware",               // 5xx already in ApiRequestLogs
        "TenantCore.Logging",
        "TenantCore.Infrastructure.Services.ErrorLoggingService",
        "TenantCore.Infrastructure.Services.ActionLoggingService"
    ];

    private readonly LogEventLevel _minimum = Enum.TryParse<LogEventLevel>(
        options.Value.MinimumLevel == "Critical" ? "Fatal" : options.Value.MinimumLevel, ignoreCase: true, out var level) && level >= LogEventLevel.Warning
        ? level
        : LogEventLevel.Error;

    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Level < _minimum)
            return;

        try
        {
            var source = logEvent.Properties.TryGetValue("SourceContext", out var sc) && sc is ScalarValue { Value: string s } ? s : "Server";
            if (SkippedSources.Any(skip => source.StartsWith(skip, StringComparison.Ordinal)))
                return;

            var http = httpContextAccessor.HttpContext;
            var status = http?.Response.StatusCode;

            logWriter.WriteAsync(options.Value.ApiErrorTable, new LogEntry
            {
                Category = LogCategories.Server,
                Source = source,
                Message = logEvent.RenderMessage(),
                ExceptionType = logEvent.Exception?.GetType().FullName,
                StackTrace = logEvent.Exception?.ToString(),
                Status = logEvent.Level == LogEventLevel.Fatal ? "Critical" : logEvent.Level.ToString(),
                RequestPath = http?.Request.Path.Value,
                HttpMethod = http?.Request.Method,
                StatusCode = status >= 400 ? status : null,
                CorrelationId = http?.TraceIdentifier,
                UserId = http?.User.FindFirst("nameid")?.Value ?? http?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                AdditionalContext = $"Level={logEvent.Level}",
                Environment = environment.EnvironmentName
            });
        }
        catch
        {
            // A sink must never throw into the code being logged.
        }
    }
}
