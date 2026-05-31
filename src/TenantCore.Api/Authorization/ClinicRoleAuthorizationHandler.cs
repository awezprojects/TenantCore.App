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

        // Resolve the active clinic: middleware already validated and stored it,
        // fall back to the first app_ids claim for single-clinic users.
        Guid applicationId;
        if (http.Items.TryGetValue(ClinicContextMiddleware.ContextKey, out var item)
            && item is Guid id && id != Guid.Empty)
        {
            applicationId = id;
        }
        else
        {
            var firstAppId = context.User.FindFirst("app_ids")?.Value;
            if (firstAppId is null || !Guid.TryParse(firstAppId, out applicationId))
                return Task.CompletedTask;
        }

        // Parse the app_roles JSON claim: [{appId,appName,roleId,roleName}]
        var appRolesClaim = context.User.FindFirst("app_roles")?.Value;
        if (string.IsNullOrEmpty(appRolesClaim)) return Task.CompletedTask;

        try
        {
            var appRoles = JsonSerializer.Deserialize<List<AppRoleClaim>>(appRolesClaim, JsonOpts);
            if (appRoles is null) return Task.CompletedTask;

            var userRoleForApp = appRoles
                .FirstOrDefault(r => r.AppId == applicationId)
                ?.RoleName;

            if (userRoleForApp is not null &&
                requirement.Roles.Any(r => string.Equals(r, userRoleForApp, StringComparison.OrdinalIgnoreCase)))
            {
                context.Succeed(requirement);
            }
        }
        catch { /* malformed claim — deny */ }

        return Task.CompletedTask;
    }

    private sealed record AppRoleClaim(Guid AppId, string AppName, Guid RoleId, string RoleName);
}
