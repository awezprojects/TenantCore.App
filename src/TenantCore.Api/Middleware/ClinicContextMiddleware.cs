namespace TenantCore.Api.Middleware;

/// <summary>
/// Reads X-Application-Id from the request, validates it against the authenticated
/// user's app_ids JWT claims, and stores the validated value in HttpContext.Items.
/// Any authenticated request that sends a header value the user is NOT linked to
/// receives 403 Forbidden immediately.
/// </summary>
public class ClinicContextMiddleware(RequestDelegate next, ILogger<ClinicContextMiddleware> logger)
{
    public const string HeaderName  = "X-Application-Id";
    public const string ContextKey  = "SelectedApplicationId";

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && context.Request.Headers.TryGetValue(HeaderName, out var headerValue)
            && !string.IsNullOrWhiteSpace(headerValue))
        {
            if (!Guid.TryParse(headerValue, out var requestedAppId))
            {
                logger.LogWarning("Invalid {Header} format: {Value}", HeaderName, (string?)headerValue);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Invalid X-Application-Id format.");
                return;
            }

            // Validate the requested app is in the user's allowed app_ids claims
            var allowedApps = context.User
                .FindAll("app_ids")
                .Select(c => c.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!allowedApps.Contains(requestedAppId.ToString()))
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
}
