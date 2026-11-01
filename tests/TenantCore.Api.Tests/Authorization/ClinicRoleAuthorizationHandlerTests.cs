using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using TenantCore.Api.Authorization;
using TenantCore.Api.Middleware;
using TenantCore.Shared.Authorization;

namespace TenantCore.Api.Tests.Authorization;

/// <summary>
/// Tests for <see cref="ClinicRoleAuthorizationHandler"/> (DEF-UM-08). The handler used to check
/// the caller's role only against the X-Application-Id HEADER, never against the route's own
/// applicationId — so a Clinic Admin of their own clinic could send their own clinic's header
/// while the route targeted a DIFFERENT clinic's resource, and the check would still pass.
/// The route's applicationId must now be the authoritative scope whenever the route has one.
/// </summary>
public class ClinicRoleAuthorizationHandlerTests
{
    private static readonly Guid ClinicA = Guid.NewGuid();
    private static readonly Guid ClinicB = Guid.NewGuid();

    [Fact]
    public async Task SystemAdmin_AlwaysSucceeds_RegardlessOfClinicContext()
    {
        var context = await RunAsync(
            requiredRoles: [AppRoles.ClinicAdmin],
            userRoles: [],
            userIsSystemAdmin: true,
            routeApplicationId: ClinicB,
            headerApplicationId: null);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task RouteApplicationId_UserHasRequiredRoleForIt_Succeeds()
    {
        var context = await RunAsync(
            requiredRoles: [AppRoles.ClinicAdmin],
            userRoles: [(ClinicA, AppRoles.ClinicAdmin)],
            userIsSystemAdmin: false,
            routeApplicationId: ClinicA,
            headerApplicationId: null);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task RouteApplicationId_NoHeaderAtAll_StillSucceeds_WhenRoleMatchesRoute()
    {
        // This is the DEF-UM-08 symptom: a raw API call with just a Bearer token and no
        // X-Application-Id header used to be denied even for a legitimate Clinic Admin of the
        // targeted clinic, because the old check had no fallback to the route's applicationId.
        var context = await RunAsync(
            requiredRoles: [AppRoles.ClinicAdmin],
            userRoles: [(ClinicA, AppRoles.ClinicAdmin)],
            userIsSystemAdmin: false,
            routeApplicationId: ClinicA,
            headerApplicationId: null);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task RouteApplicationId_HeaderForADifferentClinicTheCallerAdmins_Fails()
    {
        // The security gap: caller is Clinic Admin of ClinicA and sends X-Application-Id=ClinicA
        // (their own clinic, so ClinicContextMiddleware happily validates it), but the route
        // targets ClinicB, where the caller holds no role at all. Must be denied — the header
        // must never substitute for the route's own applicationId.
        var context = await RunAsync(
            requiredRoles: [AppRoles.ClinicAdmin],
            userRoles: [(ClinicA, AppRoles.ClinicAdmin)],
            userIsSystemAdmin: false,
            routeApplicationId: ClinicB,
            headerApplicationId: ClinicA);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task RouteApplicationId_UserHasOnlyAWeakerRoleForIt_Fails()
    {
        var context = await RunAsync(
            requiredRoles: [AppRoles.ClinicAdmin],
            userRoles: [(ClinicA, "Doctor")],
            userIsSystemAdmin: false,
            routeApplicationId: ClinicA,
            headerApplicationId: null);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task NoRouteApplicationId_FallsBackToHeaderDerivedContext_Succeeds()
    {
        // Endpoints with no applicationId in their route (e.g. PatientsController) still rely on
        // the "currently selected clinic" header/context — unaffected by this fix.
        var context = await RunAsync(
            requiredRoles: [AppRoles.ClinicAdmin],
            userRoles: [(ClinicA, AppRoles.ClinicAdmin)],
            userIsSystemAdmin: false,
            routeApplicationId: null,
            headerApplicationId: ClinicA);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task NoRouteApplicationId_NoHeaderContext_Fails()
    {
        var context = await RunAsync(
            requiredRoles: [AppRoles.ClinicAdmin],
            userRoles: [(ClinicA, AppRoles.ClinicAdmin)],
            userIsSystemAdmin: false,
            routeApplicationId: null,
            headerApplicationId: null);

        context.HasSucceeded.Should().BeFalse();
    }

    // ── Test helpers ─────────────────────────────────────────────────────────

    private static async Task<AuthorizationHandlerContext> RunAsync(
        string[] requiredRoles,
        (Guid AppId, string RoleName)[] userRoles,
        bool userIsSystemAdmin,
        Guid? routeApplicationId,
        Guid? headerApplicationId)
    {
        var httpContext = new DefaultHttpContext();

        if (routeApplicationId is { } routeId)
            httpContext.Request.RouteValues["applicationId"] = routeId.ToString();

        if (headerApplicationId is { } headerId)
            httpContext.Items[ClinicContextMiddleware.ContextKey] = headerId;

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var handler = new ClinicRoleAuthorizationHandler(accessor);

        var claims = new List<Claim>();
        if (userIsSystemAdmin)
            claims.Add(new Claim(ClaimTypes.Role, AppRoles.SystemAdmin));

        var appRolesPayload = userRoles.Select(r => new
        {
            appId = r.AppId,
            appName = "Test Clinic",
            roleId = Guid.NewGuid(),
            roleName = r.RoleName
        });
        claims.Add(new Claim("app_roles", JsonSerializer.Serialize(appRolesPayload)));

        var identity = new ClaimsIdentity(claims, authenticationType: "Test");
        var user = new ClaimsPrincipal(identity);

        var requirement = new ClinicRoleRequirement(requiredRoles);
        var authContext = new AuthorizationHandlerContext([requirement], user, resource: null);

        await handler.HandleAsync(authContext);
        return authContext;
    }
}
