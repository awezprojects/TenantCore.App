using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using TenantCore.Api.Controllers;

namespace TenantCore.Api.Middleware;

/// <summary>
/// Reads X-Application-Id from the request, validates it against the authenticated
/// user's app_ids JWT claims, and stores the validated value in HttpContext.Items.
/// Any authenticated request that sends a header value the user is NOT linked to
/// receives 403 Forbidden immediately.
///
/// An authenticated request to a clinic-scoped endpoint (any controller deriving from
/// <see cref="ClinicControllerBase"/>) that sends no header at all is also rejected with
/// 403. It used to fall through, leaving GetApplicationId() at Guid.Empty, so the query
/// simply matched nothing and the caller got an empty 200 — indistinguishable from
/// "this clinic has no records" (SEC-04).
/// </summary>
public class ClinicContextMiddleware(RequestDelegate next, ILogger<ClinicContextMiddleware> logger)
{
    public const string HeaderName  = "X-Application-Id";
    public const string ContextKey  = "SelectedApplicationId";

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var hasHeader = context.Request.Headers.TryGetValue(HeaderName, out var headerValue)
                            && !string.IsNullOrWhiteSpace(headerValue);

            if (!hasHeader)
            {
                if (IsClinicScopedEndpoint(context))
                {
                    logger.LogWarning(
                        "Authenticated request to clinic-scoped {Path} without a {Header} header",
                        context.Request.Path, HeaderName);

                    await WriteProblemAsync(
                        context,
                        "Clinic Not Selected",
                        $"This endpoint requires the {HeaderName} header. Select a clinic before calling it.");
                    return;
                }

                await next(context);
                return;
            }

            if (!Guid.TryParse(headerValue, out var requestedAppId))
            {
                logger.LogWarning("Invalid {Header} format: {Value}", HeaderName, (string?)headerValue);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Invalid X-Application-Id format.");
                return;
            }

            // Parse every app_ids claim to a proper Guid before comparing.
            // JWT claims may be uppercase/lowercase or in different string formats;
            // comparing as Guid structs avoids all string format/case issues.
            var allowedApps = context.User
                .FindAll("app_ids")
                .Select(c => Guid.TryParse(c.Value, out var g) ? (Guid?)g : null)
                .Where(g => g.HasValue)
                .Select(g => g!.Value)
                .ToHashSet();

            if (!allowedApps.Contains(requestedAppId))
            {
                var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                logger.LogWarning(
                    "User {UserId} attempted to access clinic {AppId} which is not in their allowed apps [{Allowed}]",
                    userId, requestedAppId, string.Join(", ", allowedApps));

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Access to the requested clinic is not permitted.");
                return;
            }

            context.Items[ContextKey] = requestedAppId;
        }

        await next(context);
    }

    // A clinic-scoped endpoint is any action on a controller that inherits ClinicControllerBase.
    // Global lookup controllers (DoctorSpecialities, MedicineTypes, MedicineDosageForms,
    // SubscriptionAlertSettings) and app-level controllers (Auth, Application, Clinic) derive
    // from ControllerBase and are deliberately exempt.
    private static bool IsClinicScopedEndpoint(HttpContext context)
    {
        var controllerType = context.GetEndpoint()
            ?.Metadata.GetMetadata<ControllerActionDescriptor>()?.ControllerTypeInfo.AsType();

        return controllerType is not null && typeof(ClinicControllerBase).IsAssignableFrom(controllerType);
    }

    private static async Task WriteProblemAsync(HttpContext context, string title, string detail)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: context.RequestAborted);
    }
}
