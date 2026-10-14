using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;
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
        // Phase 2 — same reasoning as /api/subscriptions: a pay-as-you-go clinic has no subscription
        // and must be able to reach the wallet screen that tops it up, and a lapsed subscription
        // clinic must still reach the plan screen that fixes it.
        "/api/usage-billing",
        "/api/subscription-alert-settings",
        "/api/auth",
        "/api/clinic/dashboard",
        "/health",
        // Clinic onboarding requests happen before any clinic (and therefore any subscription)
        // exists, so they never carry X-Application-Id — listed here defensively; none of these
        // three routes ever reaches the guard's actual check either way.
        "/api/onboarding",
        "/api/internal",
        "/api/payments/razorpay"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsGuarded(context))
        {
            await next(context);
            return;
        }

        var applicationId = (Guid)context.Items[ClinicContextMiddleware.ContextKey]!;

        // Suspension is an administrative lock-out, not a billing state, so it is checked first and
        // is deliberately NOT governed by Subscription:GuardEnabled — that switch exists to let a
        // developer work without a subscription, never to bypass a suspension.
        var accountRepository = context.RequestServices.GetRequiredService<IClinicAccountRepository>();
        if (await accountRepository.IsSuspendedAsync(applicationId, context.RequestAborted))
        {
            logger.LogWarning("Blocked request to {Path} — clinic {ApplicationId} is suspended", context.Request.Path, applicationId);

            await WriteProblemAsync(context,
                StatusCodes.Status403Forbidden,
                "Clinic Suspended",
                "This clinic has been suspended. Contact CloudClinic support.",
                SubscriptionErrorCodes.ClinicSuspended);
            return;
        }

        if (!configuration.GetValue("Subscription:GuardEnabled", true))
        {
            await next(context);
            return;
        }

        // A pay-as-you-go clinic has no subscription and never will — it is metered instead, so an
        // absent term is its normal state rather than a lock-out. It must pass the guard, or it could
        // not reach the wallet screen that tops it up. The mode comes from the same single no-tracking
        // read the UI's mode badge uses, so the guard and the screen can never disagree.
        //
        // Note the ordering: this sits *after* the unconditional suspension check above and *after*
        // the Subscription:GuardEnabled switch, so a suspended pay-as-you-go clinic is still blocked
        // with 403 clinic_suspended, and a developer working with the guard off is unaffected.
        if (await accountRepository.GetBillingModeAsync(applicationId, context.RequestAborted) == BillingMode.PayAsYouGo)
        {
            await next(context);
            return;
        }

        var subscriptionRepository = context.RequestServices.GetRequiredService<IClinicSubscriptionRepository>();
        var active = await subscriptionRepository.GetActiveForClinicAsync(applicationId, context.RequestAborted);

        if (active is not null)
        {
            await next(context);
            return;
        }

        logger.LogInformation("Blocked request to {Path} — clinic {ApplicationId} has no active subscription", context.Request.Path, applicationId);

        await WriteProblemAsync(context,
            StatusCodes.Status402PaymentRequired,
            "Subscription Required",
            "This clinic does not have an active subscription. A Clinic Admin must choose a plan before it can be used.",
            SubscriptionErrorCodes.SubscriptionRequired);
    }

    private static async Task WriteProblemAsync(HttpContext context, int status, string title, string detail, string errorCode)
    {
        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        problemDetails.Extensions["errorCode"] = errorCode;

        context.Response.StatusCode = status;
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
