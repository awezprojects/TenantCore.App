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
