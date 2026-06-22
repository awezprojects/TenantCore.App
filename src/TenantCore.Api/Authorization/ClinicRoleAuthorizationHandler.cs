using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using TenantCore.Api.Middleware;
using TenantCore.Shared.Authorization;

namespace TenantCore.Api.Authorization;

/// <summary>
/// Validates that the authenticated user holds the required role specifically within the
/// clinic identified by the X-Application-Id header (resolved by ClinicContextMiddleware).
/// SystemAdmin bypasses the per-clinic check and always succeeds.
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

        // Clinic context must have been set by ClinicContextMiddleware — no fallback.
        if (!http.Items.TryGetValue(ClinicContextMiddleware.ContextKey, out var item)
            || item is not Guid applicationId || applicationId == Guid.Empty)
            return Task.CompletedTask; // missing or invalid X-Application-Id — deny

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
