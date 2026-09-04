using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Api.Middleware;

namespace TenantCore.Api.Controllers;

public abstract class ClinicControllerBase : ControllerBase
{
    private static readonly JsonSerializerOptions _jsonOpts = new(JsonSerializerDefaults.Web);

    protected Guid GetApplicationId()
    {
        if (HttpContext.Items.TryGetValue(ClinicContextMiddleware.ContextKey, out var item)
            && item is Guid id && id != Guid.Empty)
            return id;

        return Guid.Empty;
    }

    protected Guid GetCurrentUserId()
    {
        var claim = User.FindFirst("nameid")
                 ?? User.FindFirst(ClaimTypes.NameIdentifier)
                 ?? User.FindFirst("sub");
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }

    protected string GetCurrentUserRole()
    {
        var appId = GetApplicationId();
        var claim = User.FindFirst("app_roles");
        if (claim is not null)
        {
            try
            {
                var entries = JsonSerializer.Deserialize<AppRoleEntry[]>(claim.Value, _jsonOpts);
                var role = entries?.FirstOrDefault(e => e.AppId == appId)?.RoleName;
                if (!string.IsNullOrWhiteSpace(role))
                    return role;
            }
            catch (JsonException) { }
        }
        return string.Empty;
    }

    // Unlike GetCurrentUserRole() (first match only), this returns every role the user
    // holds for the current clinic. A user can hold more than one role for the same
    // clinic — most commonly a doctor who self-registered their own clinic and is
    // therefore also that clinic's Clinic Admin — so "does this user hold role X for
    // this clinic" must check the whole set, not just whichever role happens to appear
    // first in the app_roles claim.
    protected IReadOnlyList<string> GetCurrentUserRoles()
    {
        var appId = GetApplicationId();
        var claim = User.FindFirst("app_roles");
        if (claim is not null)
        {
            try
            {
                var entries = JsonSerializer.Deserialize<AppRoleEntry[]>(claim.Value, _jsonOpts);
                if (entries is not null)
                    return entries.Where(e => e.AppId == appId).Select(e => e.RoleName).ToList();
            }
            catch (JsonException) { }
        }
        return [];
    }

    // Reads appName from the app_roles JWT claim: [{"appId":"...","appName":"..."}]
    protected string GetApplicationName()
    {
        var appId = GetApplicationId();
        var claim = User.FindFirst("app_roles");
        if (claim is not null)
        {
            try
            {
                var entries = JsonSerializer.Deserialize<AppRoleEntry[]>(claim.Value, _jsonOpts);
                var name = entries?.FirstOrDefault(e => e.AppId == appId)?.AppName;
                if (!string.IsNullOrWhiteSpace(name))
                    return Slugify(name);
            }
            catch (JsonException) { /* malformed claim — fall through */ }
        }
        return appId.ToString();
    }

    private static string Slugify(string name) =>
        System.Text.RegularExpressions.Regex
            .Replace(name.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-")
            .Trim('-');

    private sealed record AppRoleEntry(Guid AppId, string AppName, Guid RoleId, string RoleName);
}
