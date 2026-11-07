using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using TenantCore.Shared.Authorization;

namespace TenantCore.Api.Authorization;

/// <summary>
/// Validates that the authenticated user holds the required role in AT LEAST ONE of their
/// clinics — unlike <see cref="ClinicRoleAuthorizationHandler"/>, this does not require (or
/// look at) any specific clinic context. For actions on the user's own cross-clinic profile,
/// such as the doctor professional-info fields in DoctorProfileController, where there is no
/// single clinic to check against and the user may not have selected one yet.
/// </summary>
public sealed class AnyClinicRoleAuthorizationHandler : AuthorizationHandler<AnyClinicRoleRequirement>
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AnyClinicRoleRequirement requirement)
    {
        if (context.User.IsInRole(AppRoles.SystemAdmin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var appRolesClaim = context.User.FindFirst("app_roles")?.Value;
        if (string.IsNullOrEmpty(appRolesClaim)) return Task.CompletedTask;

        try
        {
            var appRoles = JsonSerializer.Deserialize<List<AppRoleClaim>>(appRolesClaim, JsonOpts);
            if (appRoles is null) return Task.CompletedTask;

            if (appRoles.Any(r => requirement.Roles.Any(role =>
                    string.Equals(role, r.RoleName, StringComparison.OrdinalIgnoreCase))))
            {
                context.Succeed(requirement);
            }
        }
        catch (JsonException) { /* malformed claim — deny */ }

        return Task.CompletedTask;
    }

    private sealed record AppRoleClaim(Guid AppId, string AppName, Guid RoleId, string RoleName);
}
