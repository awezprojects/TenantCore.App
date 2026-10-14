using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TenantCore.Application.Common;
using TenantCore.Application.Common.Behaviors;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Onboarding.Services;
using TenantCore.Application.Features.Subscriptions.Services;
using TenantCore.Application.Features.UsageBilling.Services;

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
        services.AddScoped<IClinicPlanCatalog, ClinicPlanCatalog>();

        // Phase 2 — usage billing. Registered here because they are pure Application-layer rules.
        // (UsageBillingOptions is bound in Infrastructure DI, alongside its repository registrations.)
        services.AddScoped<ITrialTermRules, TrialTermRules>();
        services.AddScoped<IUsageBillingService, UsageBillingService>();
        services.AddScoped<IBillingContactResolver, BillingContactResolver>();

        // One home for Phase 1's "at most one open clinic link" rule, so a renewal, an admin-assigned
        // plan, a wallet top-up and the registration fee all supersede identically.
        services.AddScoped<IPaymentLinkSuperseder, PaymentLinkSuperseder>();

        return services;
    }
}
