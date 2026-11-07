using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using TenantCore.Api.Authorization;
using TenantCore.Shared.Authorization;

namespace TenantCore.Api.Tests.Authorization;

/// <summary>
/// Tests for <see cref="AnyClinicRoleAuthorizationHandler"/> (DEF-MR-11). Unlike
/// <see cref="ClinicRoleAuthorizationHandler"/>, this must succeed purely from the user's
/// app_roles claim, with no clinic context (route id or X-Application-Id) at all — it backs
/// endpoints for the user's own cross-clinic profile.
/// </summary>
public class AnyClinicRoleAuthorizationHandlerTests
{
    private static readonly Guid ClinicA = Guid.NewGuid();
    private static readonly Guid ClinicB = Guid.NewGuid();

    [Fact]
    public async Task SystemAdmin_AlwaysSucceeds_WithNoAppRolesAtAll()
    {
        var context = await RunAsync(requiredRoles: [AppRoles.Doctor], userRoles: [], userIsSystemAdmin: true);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task UserHasRequiredRoleInAnyClinic_Succeeds_WithNoClinicContextAtAll()
    {
        // The exact DEF-MR-11 scenario: no clinic selected (no header, no route applicationId —
        // this handler never even looks for either), but the user holds "Doctor" in ClinicA.
        var context = await RunAsync(
            requiredRoles: AppRoles.ClinicalRoles,
            userRoles: [(ClinicA, AppRoles.Doctor)],
            userIsSystemAdmin: false);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task UserHasRequiredRoleOnlyInASecondClinic_StillSucceeds()
    {
        var context = await RunAsync(
            requiredRoles: AppRoles.ClinicalRoles,
            userRoles: [(ClinicA, AppRoles.Receptionist), (ClinicB, AppRoles.ClinicAdmin)],
            userIsSystemAdmin: false);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task UserHasNoMatchingRoleInAnyClinic_Fails()
    {
        var context = await RunAsync(
            requiredRoles: AppRoles.ClinicalRoles,
            userRoles: [(ClinicA, AppRoles.Receptionist)],
            userIsSystemAdmin: false);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task NoAppRolesClaimAtAll_Fails()
    {
        var context = await RunAsync(requiredRoles: AppRoles.ClinicalRoles, userRoles: [], userIsSystemAdmin: false);

        context.HasSucceeded.Should().BeFalse();
    }

    // ── Test helpers ─────────────────────────────────────────────────────────

    private static async Task<AuthorizationHandlerContext> RunAsync(
        string[] requiredRoles,
        (Guid AppId, string RoleName)[] userRoles,
        bool userIsSystemAdmin)
    {
        var handler = new AnyClinicRoleAuthorizationHandler();

        var claims = new List<Claim>();
        if (userIsSystemAdmin)
            claims.Add(new Claim(ClaimTypes.Role, AppRoles.SystemAdmin));

        if (userRoles.Length > 0)
        {
            var appRolesPayload = userRoles.Select(r => new
            {
                appId = r.AppId,
                appName = "Test Clinic",
                roleId = Guid.NewGuid(),
                roleName = r.RoleName
            });
            claims.Add(new Claim("app_roles", JsonSerializer.Serialize(appRolesPayload)));
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "Test");
        var user = new ClaimsPrincipal(identity);

        var requirement = new AnyClinicRoleRequirement(requiredRoles);
        var authContext = new AuthorizationHandlerContext([requirement], user, resource: null);

        await handler.HandleAsync(authContext);
        return authContext;
    }
}
