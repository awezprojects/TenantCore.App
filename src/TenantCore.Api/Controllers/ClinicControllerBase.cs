using Microsoft.AspNetCore.Mvc;
using TenantCore.Api.Middleware;

namespace TenantCore.Api.Controllers;

/// <summary>
/// Base controller for all clinic-scoped endpoints.
/// Resolves the active ApplicationId from:
///   1. HttpContext.Items["SelectedApplicationId"] — set by ClinicContextMiddleware
///      when the X-Application-Id header is present and validated against JWT claims.
///   2. Fallback: first app_ids JWT claim (single-clinic users / legacy callers).
/// </summary>
public abstract class ClinicControllerBase : ControllerBase
{
    protected Guid GetApplicationId()
    {
        // Primary: middleware-validated header value
        if (HttpContext.Items.TryGetValue(ClinicContextMiddleware.ContextKey, out var item)
            && item is Guid id && id != Guid.Empty)
            return id;

        // Fallback: first app_ids claim in JWT (single-clinic scenario)
        var claim = User.FindFirst("app_ids");
        return claim is not null && Guid.TryParse(claim.Value, out var fallback)
            ? fallback
            : Guid.Empty;
    }
}
