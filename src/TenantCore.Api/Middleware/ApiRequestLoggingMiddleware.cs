using System.Diagnostics;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using TenantCore.Logging;

namespace TenantCore.Api.Middleware;

/// <summary>
/// One ApiRequestLogs row per /api request that FAILED (status ≥ 400 — including handled 4xx that
/// never reach ApiErrorLogs), was SLOW (≥ AppLogging:SlowRequestMs), or CHANGED DATA (POST/PUT/
/// PATCH/DELETE, when AppLogging:LogMutatingRequests). On by default for every endpoint, present
/// and future (ADR-011). Records method, route template, actual status, duration, tenant, user and
/// correlation id — never request/response bodies, headers or query strings (clinical/personal
/// data). Registered right after CorrelationIdMiddleware so it sees the final status, including the
/// one ExceptionHandlingMiddleware writes. Never throws.
/// </summary>
public class ApiRequestLoggingMiddleware(
    RequestDelegate next,
    IAppLogWriter logWriter,
    IOptions<AppLoggingOptions> options,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            TryLog(context, stopwatch.ElapsedMilliseconds);
        }
    }

    public static bool ShouldLog(string method, string path, int status, long durationMs, AppLoggingOptions o)
    {
        if (status >= 400 || durationMs >= Math.Max(o.SlowRequestMs, 1))
            return true;

        // A successful browser-error report is itself a log write; logging it again is noise.
        if (path.StartsWith("/api/logs", StringComparison.OrdinalIgnoreCase))
            return false;

        return o.LogMutatingRequests && (HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method));
    }

    private void TryLog(HttpContext context, long durationMs)
    {
        try
        {
            var o = options.Value;
            var request = context.Request;
            var status = context.Response.StatusCode;
            var path = request.Path.Value ?? "/";
            if (!ShouldLog(request.Method, path, status, durationMs, o))
                return;

            var applicationId = context.Items.TryGetValue(ClinicContextMiddleware.ContextKey, out var item) && item is Guid id && id != Guid.Empty
                ? id.ToString()
                : null;
            var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText;

            logWriter.WriteAsync(o.RequestLogTable, new LogEntry
            {
                Category = LogCategories.Request,
                Source = "Api.Request",
                Message = $"{request.Method} {path} → {status} in {durationMs} ms",
                RequestPath = path,
                HttpMethod = request.Method,
                StatusCode = status,
                DurationMs = durationMs,
                CorrelationId = context.TraceIdentifier,
                ApplicationId = applicationId,
                UserId = context.User.FindFirst("nameid")?.Value ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                AdditionalContext = route is null ? null : $"Route={route}",
                Environment = environment.EnvironmentName
            });
        }
        catch
        {
            // Logging must never break the request it describes.
        }
    }
}
