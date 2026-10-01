using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TenantCore.Api.Middleware;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Api.Tests.Middleware;

public class SubscriptionGuardMiddlewareTests
{
    private readonly Mock<IClinicSubscriptionRepository> _subscriptionRepository = new();
    private readonly Mock<IClinicAccountRepository> _accountRepository = new();
    private readonly Guid _applicationId = Guid.NewGuid();

    public SubscriptionGuardMiddlewareTests()
        => _accountRepository.Setup(r => r.IsSuspendedAsync(_applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

    private SubscriptionGuardMiddleware CreateMiddleware(RequestDelegate next, bool guardEnabled = true)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Subscription:GuardEnabled"] = guardEnabled.ToString() })
            .Build();

        return new SubscriptionGuardMiddleware(next, configuration, NullLogger<SubscriptionGuardMiddleware>.Instance);
    }

    private DefaultHttpContext CreateAuthenticatedContext(string path = "/api/patients")
    {
        var services = new ServiceCollection();
        services.AddSingleton(_subscriptionRepository.Object);
        services.AddSingleton(_accountRepository.Object);

        var context = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test-user")], "TestAuth"))
        };
        context.Request.Path = path;
        context.Items[ClinicContextMiddleware.ContextKey] = _applicationId;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private ClinicSubscription CurrentTerm()
    {
        var plan = SubscriptionPlan.CreateForSeed(
            Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", "d", 30, 999m, "INR", false, false, 1);
        return ClinicSubscription.Create(_applicationId, plan, DateTime.UtcNow.AddDays(-5), "C", "a@b.com", "A");
    }

    private static async Task<string> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return await new StreamReader(context.Response.Body).ReadToEndAsync();
    }

    [Fact]
    public async Task InvokeAsync_CurrentTerm_CallsNext()
    {
        _subscriptionRepository.Setup(r => r.GetActiveForClinicAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CurrentTerm());
        var context = CreateAuthenticatedContext();
        var nextCalled = false;

        await CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }).InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_NoCurrentTerm_BlocksWith402()
    {
        // GetActiveForClinicAsync excludes terms that have not started, so a clinic holding only an
        // upcoming term lands here — correctly locked until that term begins.
        _subscriptionRepository.Setup(r => r.GetActiveForClinicAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicSubscription?)null);
        var context = CreateAuthenticatedContext();
        var nextCalled = false;

        await CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }).InvokeAsync(context);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status402PaymentRequired);
        (await ReadBodyAsync(context)).Should().Contain("subscription_required");
    }

    [Fact]
    public async Task InvokeAsync_SuspendedClinic_BlocksWith403AndTheSuspendedErrorCode()
    {
        _accountRepository.Setup(r => r.IsSuspendedAsync(_applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _subscriptionRepository.Setup(r => r.GetActiveForClinicAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CurrentTerm());
        var context = CreateAuthenticatedContext();
        var nextCalled = false;

        await CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }).InvokeAsync(context);

        nextCalled.Should().BeFalse("a suspension blocks the clinic even while its subscription is valid");
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        (await ReadBodyAsync(context)).Should().Contain("clinic_suspended");
    }

    [Fact]
    public async Task InvokeAsync_SuspendedClinic_IsBlockedEvenWhenTheSubscriptionGuardIsDisabled()
    {
        // Subscription:GuardEnabled=false exists so a developer can work without a subscription.
        // It must never double as a way around an administrative suspension.
        _accountRepository.Setup(r => r.IsSuspendedAsync(_applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var context = CreateAuthenticatedContext();
        var nextCalled = false;

        await CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, guardEnabled: false)
            .InvokeAsync(context);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task InvokeAsync_GuardDisabledAndNotSuspended_CallsNextWithoutCheckingTheSubscription()
    {
        var context = CreateAuthenticatedContext();
        var nextCalled = false;

        await CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, guardEnabled: false)
            .InvokeAsync(context);

        nextCalled.Should().BeTrue();
        _subscriptionRepository.Verify(r => r.GetActiveForClinicAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("/api/subscriptions/status")]
    [InlineData("/api/subscriptions/payments")]
    [InlineData("/api/auth/me")]
    [InlineData("/health")]
    [InlineData("/api/internal/clinics/x/suspend")]
    public async Task InvokeAsync_ExemptPath_CallsNextWithoutAnyCheck(string path)
    {
        // These must stay reachable for a locked or suspended clinic — otherwise it could never
        // read its own status or pay to unlock itself.
        _accountRepository.Setup(r => r.IsSuspendedAsync(_applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var context = CreateAuthenticatedContext(path);
        var nextCalled = false;

        await CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }).InvokeAsync(context);

        nextCalled.Should().BeTrue();
        _accountRepository.Verify(r => r.IsSuspendedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_UnauthenticatedRequest_PassesThrough()
    {
        var context = CreateAuthenticatedContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity());
        var nextCalled = false;

        await CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }).InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_NoClinicContext_PassesThrough()
    {
        var context = CreateAuthenticatedContext();
        context.Items.Remove(ClinicContextMiddleware.ContextKey);
        var nextCalled = false;

        await CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }).InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }
}
