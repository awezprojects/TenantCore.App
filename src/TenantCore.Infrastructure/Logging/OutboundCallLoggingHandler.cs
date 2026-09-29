using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TenantCore.Logging;

namespace TenantCore.Infrastructure.Logging;

/// <summary>
/// Logs EVERY outbound HTTP call made through IHttpClientFactory (Razorpay, TenantCore.Auth, and any
/// client added later — attached via ConfigureHttpClientDefaults, ADR-011) as one ActionLogs row:
/// "Outbound {host}", method, path (query string dropped), status received, duration and the current
/// request's correlation id. Status ≥ 400 or an exception = "Failed". Never logs headers or bodies
/// (Razorpay basic-auth keys, bearer tokens, payer details). Never throws its own errors.
/// </summary>
public sealed class OutboundCallLoggingHandler(
    IAppLogWriter logWriter,
    IOptions<AppLoggingOptions> options,
    IHostEnvironment environment,
    IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            stopwatch.Stop();
            Write(request, (int)response.StatusCode, stopwatch.ElapsedMilliseconds, error: null);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Write(request, null, stopwatch.ElapsedMilliseconds, ex is OperationCanceledException ? "Cancelled or timed out" : ex.Message);
            throw;
        }
    }

    private void Write(HttpRequestMessage request, int? status, long durationMs, string? error)
    {
        try
        {
            var uri = request.RequestUri;
            var host = uri is null ? "(unknown)" : uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
            var path = uri?.AbsolutePath ?? "/";   // AbsolutePath excludes the query string
            var failed = error is not null || status >= 400;
            var http = httpContextAccessor.HttpContext;

            logWriter.WriteAsync(options.Value.ActionLogTable, new LogEntry
            {
                Category = LogCategories.Outbound,
                Source = $"Outbound {host}",
                Message = error is null
                    ? $"{request.Method} {host}{path} → {status} in {durationMs} ms"
                    : $"{request.Method} {host}{path} failed after {durationMs} ms: {error}",
                RequestPath = path,
                HttpMethod = request.Method.Method,
                StatusCode = status,
                DurationMs = durationMs,
                Status = failed ? "Failed" : "Completed",
                CorrelationId = http?.TraceIdentifier,
                UserId = http?.User.FindFirst("nameid")?.Value ?? http?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                AdditionalContext = $"OutboundCall | host={host}",
                Environment = environment.EnvironmentName
            });
        }
        catch
        {
            // Logging must never break the call it describes.
        }
    }
}
