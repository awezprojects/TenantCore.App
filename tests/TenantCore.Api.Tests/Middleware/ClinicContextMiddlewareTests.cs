using System.Reflection;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging.Abstractions;
using TenantCore.Api.Controllers;
using TenantCore.Api.Middleware;

namespace TenantCore.Api.Tests.Middleware;

/// <summary>
/// Covers the X-Application-Id contract: a valid, owned header is stashed for the controller;
/// an owned-but-invalid header is rejected; and — the SEC-04 gap — an authenticated call to a
/// clinic-scoped endpoint with NO header at all is rejected with 403 instead of silently
/// running against Guid.Empty and returning an empty 200.
/// </summary>
public class ClinicContextMiddlewareTests
{
    private readonly Guid _applicationId = Guid.NewGuid();

    private static ClinicContextMiddleware CreateMiddleware(RequestDelegate next) =>
        new(next, NullLogger<ClinicContextMiddleware>.Instance);

    private HttpContext CreateContext(bool authenticated, string? appIdHeader, Type? controllerType)
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, "test-user") };
        if (authenticated) claims.Add(new Claim("app_ids", _applicationId.ToString()));

        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticated ? "TestAuth" : null))
        };

        if (appIdHeader is not null)
            context.Request.Headers[ClinicContextMiddleware.HeaderName] = appIdHeader;

        if (controllerType is not null)
        {
            var descriptor = new ControllerActionDescriptor { ControllerTypeInfo = controllerType.GetTypeInfo() };
            context.SetEndpoint(new Endpoint(
                null, new EndpointMetadataCollection(descriptor), controllerType.Name));
        }

        return context;
    }

    [Fact]
    public async Task InvokeAsync_AuthenticatedClinicScopedRouteWithoutHeader_Returns403WithProblemDetails()
    {
        var context = CreateContext(authenticated: true, appIdHeader: null, typeof(PatientsController));
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        context.Response.ContentType.Should().Be("application/problem+json");
        context.Items.ContainsKey(ClinicContextMiddleware.ContextKey).Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_AuthenticatedGlobalLookupRouteWithoutHeader_PassesThrough()
    {
        var context = CreateContext(authenticated: true, appIdHeader: null, typeof(DoctorSpecialitiesController));
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_NoEndpointResolvedWithoutHeader_PassesThrough()
    {
        var context = CreateContext(authenticated: true, appIdHeader: null, controllerType: null);
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_UnauthenticatedWithoutHeader_PassesThrough()
    {
        var context = CreateContext(authenticated: false, appIdHeader: null, typeof(PatientsController));
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_OwnedHeader_StoresApplicationIdForTheController()
    {
        var context = CreateContext(authenticated: true, appIdHeader: _applicationId.ToString(), typeof(PatientsController));
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Items[ClinicContextMiddleware.ContextKey].Should().Be(_applicationId);
    }

    [Fact]
    public async Task InvokeAsync_HeaderNotOwnedByUser_Returns403()
    {
        var context = CreateContext(authenticated: true, appIdHeader: Guid.NewGuid().ToString(), typeof(PatientsController));
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task InvokeAsync_MalformedHeader_Returns400()
    {
        var context = CreateContext(authenticated: true, appIdHeader: "not-a-guid", typeof(PatientsController));
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }
}