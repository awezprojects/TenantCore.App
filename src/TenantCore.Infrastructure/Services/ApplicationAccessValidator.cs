using System.Text.Json;
using Microsoft.AspNetCore.Http;
using TenantCore.Application.Common;
using TenantCore.Shared.Authorization;

namespace TenantCore.Infrastructure.Services;

public sealed class ApplicationAccessValidator(IHttpContextAccessor httpContextAccessor)
    : IApplicationAccessValidator
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public bool CanAccess(Guid applicationId)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true) return false;

        var allowedApps = user.FindAll("app_ids")
            .Select(c => Guid.TryParse(c.Value, out var g) ? (Guid?)g : null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToHashSet();

        // No app_ids in token means no clinic linkage — deny
        if (allowedApps.Count == 0) return false;

        return allowedApps.Contains(applicationId);
    }

    // Mirrors ClinicRoleAuthorizationHandler's own app_roles parsing exactly, scoped to an
    // explicit applicationId instead of a route/header-derived one — see the interface doc for why.
    public bool HasRole(Guid applicationId, IEnumerable<string> roles)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true) return false;

        if (user.IsInRole(AppRoles.SystemAdmin)) return true;

        var appRolesClaim = user.FindFirst("app_roles")?.Value;
        if (string.IsNullOrEmpty(appRolesClaim)) return false;

        try
        {
            var appRoles = JsonSerializer.Deserialize<List<AppRoleClaim>>(appRolesClaim, JsonOpts);
            if (appRoles is null) return false;

            var roleSet = roles.ToHashSet(StringComparer.OrdinalIgnoreCase);
            return appRoles.Any(r => r.AppId == applicationId && roleSet.Contains(r.RoleName));
        }
        catch (JsonException)
        {
            return false; // malformed claim — deny
        }
    }

    private sealed record AppRoleClaim(Guid AppId, string AppName, Guid RoleId, string RoleName);
}
