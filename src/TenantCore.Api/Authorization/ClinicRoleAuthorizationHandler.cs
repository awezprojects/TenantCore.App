using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using TenantCore.Api.Middleware;
using TenantCore.Shared.Authorization;

namespace TenantCore.Api.Authorization;

/// <summary>
/// Validates that the authenticated user holds the required role specifically within the
/// target clinic. SystemAdmin bypasses the per-clinic check and always succeeds.
/// </summary>
public sealed class ClinicRoleAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<ClinicRoleRequirement>
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ClinicRoleRequirement requirement)
    {
        // System Admin is cross-application — always passes
        if (context.User.IsInRole(AppRoles.SystemAdmin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var http = httpContextAccessor.HttpContext;
        if (http is null) return Task.CompletedTask; // fails by default

        // When the route itself targets a specific clinic (e.g. DELETE
        // /api/Application/{applicationId}/invitations/{id}), that route value is the
        // resource being acted on and MUST be the id checked here. Checking the
        // X-Application-Id header instead (as this used to) let a Clinic Admin of their
        // OWN clinic send their own clinic's header while the route targeted a DIFFERENT
        // clinic's resource — the two were never required to match, so any Clinic Admin
        // could mutate any other clinic's applications/users/invitations (DEF-UM-08's raw
        // 403-vs-SPA-200 symptom was this same gap's harmless side; a mismatched-but-valid
        // header was the harmful side). Only fall back to the header-derived context (set
        // by ClinicContextMiddleware) for endpoints with no applicationId in their route at
        // all, which rely on "the currently selected clinic" instead of a route id.
        Guid applicationId;
        if (http.GetRouteValue("applicationId") is { } routeValue
            && Guid.TryParse(routeValue.ToString(), out var routeApplicationId))
        {
            applicationId = routeApplicationId;
        }
        else if (http.Items.TryGetValue(ClinicContextMiddleware.ContextKey, out var item)
                 && item is Guid headerApplicationId && headerApplicationId != Guid.Empty)
        {
            applicationId = headerApplicationId;
        }
        else
        {
            return Task.CompletedTask; // no applicable clinic context — deny
        }

        // Parse the app_roles JSON claim: [{appId,appName,roleId,roleName}]
        var appRolesClaim = context.User.FindFirst("app_roles")?.Value;
        if (string.IsNullOrEmpty(appRolesClaim)) return Task.CompletedTask;

        try
        {
            var appRoles = JsonSerializer.Deserialize<List<AppRoleClaim>>(appRolesClaim, JsonOpts);
            if (appRoles is null) return Task.CompletedTask;

            // A user may hold multiple roles for the same clinic (e.g. Doctor + Clinic Admin).
            // Check all roles for this application, not just the first one.
            var userRolesForApp = appRoles
                .Where(r => r.AppId == applicationId)
                .Select(r => r.RoleName)
                .ToList();

            if (userRolesForApp.Any(roleName =>
                    requirement.Roles.Any(r => string.Equals(r, roleName, StringComparison.OrdinalIgnoreCase))))
            {
                context.Succeed(requirement);
            }
        }
        catch (JsonException) { /* malformed claim — deny */ }

        return Task.CompletedTask;
    }

    private sealed record AppRoleClaim(Guid AppId, string AppName, Guid RoleId, string RoleName);
}
