using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TenantCore.Application.Services;
using TenantCore.Logging;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Services;

/// <summary>
/// Adapter boundary: Domain/Application never see <see cref="TenantCore.Logging"/> types
/// directly. Builds a <see cref="LogEntry"/> from the Application-facing call, resolves
/// the target table from <see cref="LogCategory"/>, and delegates to <see cref="IAppLogWriter"/>.
/// Never throws — a logging failure must not break the operation being logged.
/// </summary>
public sealed class ErrorLoggingService(
    IAppLogWriter logWriter,
    IOptions<AppLoggingOptions> options,
    IHostEnvironment environment,
    ILogger<ErrorLoggingService> logger,
    IHttpContextAccessor? httpContextAccessor = null)
    : IErrorLogger
{
    public async Task LogAsync(
        LogCategory category,
        string source,
        string message,
        string? exceptionType = null,
        string? stackTrace = null,
        Guid? applicationId = null,
        string? userId = null,
        string? additionalContext = null,
        CancellationToken ct = default)
    {
        try
        {
            var tableName = category switch
            {
                LogCategory.Frontend => options.Value.FrontendErrorTable,
                _ => options.Value.ApiErrorTable
            };

            // The request this error happened in (null for background work). Browser errors arrive
            // through POST api/logs/frontend — that request's path says nothing about the error.
            var http = category == LogCategory.Frontend ? null : httpContextAccessor?.HttpContext;
            var status = http?.Response.StatusCode;

            var entry = new LogEntry
            {
                Category = category.ToString(),
                Source = source,
                Message = message,
                ExceptionType = exceptionType,
                StackTrace = stackTrace,
                ApplicationId = applicationId is null || applicationId == Guid.Empty ? null : applicationId.ToString(),
                UserId = userId,
                AdditionalContext = additionalContext,
                Environment = environment.EnvironmentName,
                RequestPath = http?.Request.Path.Value,
                HttpMethod = http?.Request.Method,
                CorrelationId = http?.TraceIdentifier,
                // Only a status the middleware has already decided on (it sets it before logging).
                StatusCode = status >= 400 ? status : null
            };

            await logWriter.WriteAsync(tableName, entry, ct);
        }
        catch (Exception ex)
        {
            // Logging must never break the caller's operation.
            logger.LogWarning(ex, "Failed to write error log entry (Category={Category}, Source={Source})", category, source);
        }
    }
}
