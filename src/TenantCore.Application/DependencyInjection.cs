using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TenantCore.Application.Common;
using TenantCore.Application.Common.Behaviors;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Onboarding.Services;

namespace TenantCore.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
        });

        services.AddValidatorsFromAssembly(assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ActionLoggingBehavior<,>));

        services.Configure<WorkflowOptions>(configuration.GetSection("Workflow"));
        services.Configure<OnboardingOptions>(configuration.GetSection("Onboarding"));

        services.AddScoped<IWorkflowEnqueuer, WorkflowEnqueuer>();
        services.AddScoped<IOnboardingSelfHealer, OnboardingSelfHealer>();

        return services;
    }
}
