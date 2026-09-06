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
    ILogger<ErrorLoggingService> logger)
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
                Environment = environment.EnvironmentName
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
