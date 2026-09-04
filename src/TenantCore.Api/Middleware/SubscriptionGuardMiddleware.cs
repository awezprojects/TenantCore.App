using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Errors;

namespace TenantCore.Api.Middleware;

/// <summary>
/// Blocks every clinic-scoped request when the current clinic has no active
/// subscription. Runs after ClinicContextMiddleware (so HttpContext.Items holds
/// the validated ApplicationId) and before UseAuthorization(), per ADR-005 —
/// this is an addition to the pipeline, not a reorder.
///
/// A request only reaches the check below when the caller sent a validated
/// X-Application-Id — unauthenticated requests and requests with no clinic
/// header (e.g. cross-clinic listing endpoints) pass straight through, exactly
/// as they did before this middleware existed.
/// </summary>
public class SubscriptionGuardMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<SubscriptionGuardMiddleware> logger)
{
    // Routes that must stay reachable even for a locked clinic — otherwise a
    // clinic could never reach the screen that unlocks it.
    private static readonly string[] ExemptPathPrefixes =
    [
        "/api/subscriptions",
        "/api/subscription-alert-settings",
        "/api/auth",
        "/api/clinic/dashboard",
        "/health"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsGuarded(context))
        {
            await next(context);
            return;
        }

        if (!configuration.GetValue("Subscription:GuardEnabled", true))
        {
            await next(context);
            return;
        }

        var applicationId = (Guid)context.Items[ClinicContextMiddleware.ContextKey]!;
        var subscriptionRepository = context.RequestServices.GetRequiredService<IClinicSubscriptionRepository>();
        var active = await subscriptionRepository.GetActiveForClinicAsync(applicationId, context.RequestAborted);

        if (active is not null)
        {
            await next(context);
            return;
        }

        logger.LogInformation("Blocked request to {Path} — clinic {ApplicationId} has no active subscription", context.Request.Path, applicationId);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status402PaymentRequired,
            Title = "Subscription Required",
            Detail = "This clinic does not have an active subscription. A Clinic Admin must choose a plan before it can be used.",
            Instance = context.Request.Path
        };
        problemDetails.Extensions["errorCode"] = SubscriptionErrorCodes.SubscriptionRequired;

        context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
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
        return !ExemptPathPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
