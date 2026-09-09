using Microsoft.AspNetCore.Mvc;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Errors;

namespace TenantCore.Api.Middleware;

/// <summary>
/// Blocks every request to a billing-related route when the current clinic has
/// disabled billing (ClinicFeatureFlags.BillingEnabled = false). Runs after
/// ClinicContextMiddleware (so HttpContext.Items holds the validated
/// ApplicationId) and alongside SubscriptionGuardMiddleware — an addition to
/// the pipeline, not a reorder, per ADR-005.
///
/// This is the real security boundary: even a direct API call bypassing the
/// Blazor client is rejected, not just hidden nav links.
/// </summary>
public class BillingFeatureGuardMiddleware(RequestDelegate next, ILogger<BillingFeatureGuardMiddleware> logger)
{
    // Every route prefix that exposes billing/financial data or actions.
    private static readonly string[] GuardedPathPrefixes =
    [
        "/api/opd-payments",
        "/api/opd-particulars",
        "/api/particulars",
        "/api/finance-reports",
        "/api/counter-sessions",
        "/api/expense-categories",
        "/api/expense-records",
        "/api/amount-handovers"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsGuarded(context))
        {
            await next(context);
            return;
        }

        var applicationId = (Guid)context.Items[ClinicContextMiddleware.ContextKey]!;
        var featureFlagsRepository = context.RequestServices.GetRequiredService<IClinicFeatureFlagsRepository>();
        var flags = await featureFlagsRepository.GetByApplicationAsync(applicationId, context.RequestAborted);

        if (flags?.BillingEnabled ?? true)
        {
            await next(context);
            return;
        }

        logger.LogInformation("Blocked request to {Path} — clinic {ApplicationId} has billing disabled", context.Request.Path, applicationId);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Billing Disabled",
            Detail = "This clinic has disabled billing. A Clinic Admin must re-enable it in Settings before this action is available.",
            Instance = context.Request.Path
        };
        problemDetails.Extensions["errorCode"] = BillingErrorCodes.BillingDisabled;

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private static bool IsGuarded(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return false;

        if (!context.Items.TryGetValue(ClinicContextMiddleware.ContextKey, out var item)
            || item is not Guid applicationId || applicationId == Guid.Empty)
            return false;

        var path = context.Request.Path.Value ?? string.Empty;
        return GuardedPathPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
