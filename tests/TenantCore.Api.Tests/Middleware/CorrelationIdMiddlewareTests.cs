using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using TenantCore.Api.Middleware;

namespace TenantCore.Api.Tests.Middleware;

public class CorrelationIdMiddlewareTests
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    [Fact]
    public async Task InvokeAsync_WhenNoHeaderProvided_GeneratesNewGuidAndSetsResponseHeader()
    {
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers.TryGetValue(CorrelationIdHeader, out var headerValue)
            .Should().BeTrue();
        Guid.TryParse(headerValue.ToString(), out _).Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenNoHeaderProvided_SetsTraceIdentifierToGeneratedGuid()
    {
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Guid.TryParse(context.TraceIdentifier, out _).Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenHeaderProvided_UsesProvidedCorrelationId()
    {
        const string correlationId = "test-correlation-id-abc";
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdHeader] = correlationId;
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.TraceIdentifier.Should().Be(correlationId);
        context.Response.Headers[CorrelationIdHeader].ToString().Should().Be(correlationId);
    }

    [Fact]
    public async Task InvokeAsync_AlwaysCallsNextDelegate()
    {
        var context = new DefaultHttpContext();
        var nextCalled = false;
        var middleware = new CorrelationIdMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_CorrelationIdInResponseMatchesTraceIdentifier()
    {
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var responseCorrelationId = context.Response.Headers[CorrelationIdHeader].ToString();
        responseCorrelationId.Should().Be(context.TraceIdentifier);
    }
}
