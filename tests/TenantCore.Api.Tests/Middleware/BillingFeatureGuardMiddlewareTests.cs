using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TenantCore.Api.Middleware;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Api.Tests.Middleware;

public class BillingFeatureGuardMiddlewareTests
{
    private readonly Mock<IClinicFeatureFlagsRepository> _featureFlagsRepository = new();
    private readonly Guid _applicationId = Guid.NewGuid();

    private BillingFeatureGuardMiddleware CreateMiddleware(RequestDelegate next) =>
        new(next, NullLogger<BillingFeatureGuardMiddleware>.Instance);

    private DefaultHttpContext CreateAuthenticatedContext(string path)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_featureFlagsRepository.Object);

        var context = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test-user")], "TestAuth"))
        };
        context.Request.Path = path;
        context.Items[ClinicContextMiddleware.ContextKey] = _applicationId;
        return context;
    }

    [Theory]
    [InlineData("/api/opd-payments")]
    [InlineData("/api/opd-particulars")]
    [InlineData("/api/particulars")]
    [InlineData("/api/finance-reports")]
    [InlineData("/api/counter-sessions")]
    [InlineData("/api/expense-categories")]
    [InlineData("/api/expense-records")]
    [InlineData("/api/amount-handovers")]
    public async Task InvokeAsync_BillingDisabled_BlocksGuardedRouteWith403(string path)
    {
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ClinicFeatureFlags.Create(_applicationId, true, false));
        var context = CreateAuthenticatedContext(path);
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Theory]
    [InlineData("/api/opd-payments")]
    [InlineData("/api/counter-sessions")]
    public async Task InvokeAsync_BillingEnabled_AllowsGuardedRoute(string path)
    {
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ClinicFeatureFlags.Create(_applicationId, true, true));
        var context = CreateAuthenticatedContext(path);
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_NoFeatureFlagsRow_DefaultsToBillingEnabled()
    {
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicFeatureFlags?)null);
        var context = CreateAuthenticatedContext("/api/finance-reports");
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_UnguardedRoute_PassesThroughWithoutCheckingFlags()
    {
        var context = CreateAuthenticatedContext("/api/patients");
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        _featureFlagsRepository.Verify(r => r.GetByApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_FeatureFlagsEndpointItself_StaysExempt()
    {
        var context = CreateAuthenticatedContext("/api/clinic-settings/feature-flags");
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        _featureFlagsRepository.Verify(r => r.GetByApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_Unauthenticated_PassesThroughWithoutCheckingFlags()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_featureFlagsRepository.Object);
        var context = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        context.Request.Path = "/api/opd-payments";

        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        _featureFlagsRepository.Verify(r => r.GetByApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_NoClinicContextItem_PassesThroughWithoutCheckingFlags()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_featureFlagsRepository.Object);
        var context = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test-user")], "TestAuth"))
        };
        context.Request.Path = "/api/opd-payments";

        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        _featureFlagsRepository.Verify(r => r.GetByApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
