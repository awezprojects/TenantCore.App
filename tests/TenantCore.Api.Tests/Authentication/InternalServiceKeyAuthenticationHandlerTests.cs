using System.Text.Encodings.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TenantCore.Api.Authentication;

namespace TenantCore.Api.Tests.Authentication;

/// <summary>
/// Tests for <see cref="InternalServiceKeyAuthenticationHandler"/> — the "InternalService" scheme
/// guarding api/internal/* for the Admin portal. A correct X-Internal-Service-Key authenticates
/// with role=InternalService; a wrong or missing key fails authentication without throwing.
///
/// This file lives here (rather than under tests/TenantCore.Infrastructure.Tests/) because the
/// handler itself lives in TenantCore.Api (Infrastructure has no ASP.NET Core Authentication
/// package reference — see the handler's own doc comment). TenantCore.Api.Tests already
/// references TenantCore.Api.csproj and the ASP.NET Core shared framework, and TenantCore.Api
/// does not reference this test project, so there is no circular reference either way.
/// See plan/clinic-trial-razorpay-subscriptions/PLAN.md.
/// </summary>
public class InternalServiceKeyAuthenticationHandlerTests
{
    private const string HeaderName = "X-Internal-Service-Key";
    private const string ConfiguredKey = "correct-shared-key-value";

    [Fact]
    public async Task AuthenticateAsync_CorrectKey_Succeeds_WithInternalServiceRoleClaim()
    {
        var handler = await CreateHandlerAsync(CreateContext(ConfiguredKey));

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeTrue();
        result.Principal.Should().NotBeNull();
        result.Principal!.IsInRole("InternalService").Should().BeTrue();
        result.Principal.Identity!.Name.Should().Be("AdminPortal");
        result.Principal.Identity.AuthenticationType.Should().Be(InternalServiceKeyAuthenticationHandler.SchemeName);
        result.Ticket!.AuthenticationScheme.Should().Be(InternalServiceKeyAuthenticationHandler.SchemeName);
    }

    [Fact]
    public async Task AuthenticateAsync_WrongKey_FailsWithoutThrowing()
    {
        var handler = await CreateHandlerAsync(CreateContext("some-other-key"));

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        result.Failure.Should().NotBeNull();
        result.Principal.Should().BeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_MissingHeader_FailsWithoutThrowing()
    {
        var handler = await CreateHandlerAsync(CreateContext(headerValue: null));

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        result.Failure.Should().NotBeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_EmptyHeaderValue_FailsWithoutThrowing()
    {
        var handler = await CreateHandlerAsync(CreateContext(""));

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task AuthenticateAsync_AdminPortalKeyNotConfigured_FailsWithoutThrowing()
    {
        var handler = await CreateHandlerAsync(CreateContext(ConfiguredKey), configuredKey: "");

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        result.Failure.Should().NotBeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_ProvidedKeyOfDifferentLength_FailsWithoutThrowing()
    {
        // Exercises the length-mismatch short-circuit of the fixed-time comparison specifically,
        // distinct from the equal-length-but-different-content "wrong key" case above.
        var handler = await CreateHandlerAsync(CreateContext(ConfiguredKey + "-extra-suffix"));

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task AuthenticateAsync_CaseSensitive_DoesNotAcceptDifferentCasing()
    {
        var handler = await CreateHandlerAsync(CreateContext(ConfiguredKey.ToUpperInvariant()));

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
    }

    // ── Test helpers ─────────────────────────────────────────────────────────

    private static HttpContext CreateContext(string? headerValue)
    {
        var context = new DefaultHttpContext();
        if (headerValue != null)
            context.Request.Headers[HeaderName] = headerValue;
        return context;
    }

    private static async Task<InternalServiceKeyAuthenticationHandler> CreateHandlerAsync(HttpContext context, string configuredKey = ConfiguredKey)
    {
        var schemeOptionsMonitor = new StaticOptionsMonitor<AuthenticationSchemeOptions>(new AuthenticationSchemeOptions());
        var internalApiOptions = Options.Create(new InternalApiOptions { AdminPortalKey = configuredKey });

        var handler = new InternalServiceKeyAuthenticationHandler(
            schemeOptionsMonitor, NullLoggerFactory.Instance, UrlEncoder.Default, internalApiOptions);

        var scheme = new AuthenticationScheme(
            InternalServiceKeyAuthenticationHandler.SchemeName,
            InternalServiceKeyAuthenticationHandler.SchemeName,
            typeof(InternalServiceKeyAuthenticationHandler));

        await handler.InitializeAsync(scheme, context);
        return handler;
    }

    private sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue { get; } = value;
        public T Get(string? name) => CurrentValue;
        public IDisposable OnChange(Action<T, string?> listener) => NullDisposable.Instance;

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();
            public void Dispose() { }
        }
    }
}
