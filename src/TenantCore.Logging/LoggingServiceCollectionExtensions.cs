using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TenantCore.Logging;

public static class LoggingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the low-level log writer. This is the single place a future provider
    /// swap (Coralogix, Application Insights) happens — everything that depends on
    /// <see cref="IAppLogWriter"/> is unaffected by that change.
    /// Callers get <see cref="QueuedAppLogWriter"/> (enqueue and return); the background
    /// <see cref="AppLogQueueService"/> performs the actual Table Storage writes.
    /// </summary>
    public static IServiceCollection AddAppLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AppLoggingOptions>(configuration.GetSection(AppLoggingOptions.SectionName));
        services.AddSingleton<AzureTableLogWriter>();
        services.AddSingleton<AppLogQueue>();
        services.AddSingleton<IAppLogWriter, QueuedAppLogWriter>();
        services.AddHostedService<AppLogQueueService>();
        return services;
    }
}
