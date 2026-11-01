using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TenantCore.Infrastructure.Logging;
using TenantCore.Infrastructure.Services;
using TenantCore.Logging;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Tests.Logging;

/// <summary>ADR-011: outbound calls, error-row HTTP columns, queueing and scrubbing.</summary>
public class LoggingPipelineTests
{
    private static IHostEnvironment Env()
    {
        var env = new Mock<IHostEnvironment>();
        env.SetupGet(e => e.EnvironmentName).Returns("Testing");
        return env.Object;
    }

    private static (Mock<IAppLogWriter> Writer, List<(string Table, LogEntry Entry)> Rows) Writer()
    {
        var rows = new List<(string, LogEntry)>();
        var writer = new Mock<IAppLogWriter>();
        writer.Setup(w => w.WriteAsync(It.IsAny<string>(), It.IsAny<LogEntry>(), It.IsAny<CancellationToken>()))
            .Callback<string, LogEntry, CancellationToken>((t, e, _) => rows.Add((t, e)))
            .Returns(Task.CompletedTask);
        return (writer, rows);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }

    private static HttpClient Client(IAppLogWriter writer, Func<HttpRequestMessage, HttpResponseMessage> respond, HttpContext? http = null) =>
        new(new OutboundCallLoggingHandler(writer, Options.Create(new AppLoggingOptions()), Env(), new HttpContextAccessor { HttpContext = http })
        {
            InnerHandler = new StubHandler(respond)
        });

    // ---------------------------------------------------------------- outbound calls

    [Fact]
    public async Task Outbound_SuccessfulCall_IsACompletedActionRow_WithoutQueryStringOrHeaders()
    {
        var (writer, rows) = Writer();
        var http = new DefaultHttpContext { TraceIdentifier = "corr-9" };
        var client = Client(writer.Object, _ => new HttpResponseMessage(HttpStatusCode.OK), http);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.razorpay.com/v1/payment_links?reference_id=ref-1");
        request.Headers.TryAddWithoutValidation("Authorization", "Basic c2VjcmV0OmtleQ==");

        await client.SendAsync(request);

        var (table, row) = rows.Should().ContainSingle().Subject;
        table.Should().Be("ActionLogs");
        row.Category.Should().Be("Outbound");
        row.Source.Should().Be("Outbound api.razorpay.com");
        row.Status.Should().Be("Completed");
        row.HttpMethod.Should().Be("GET");
        row.StatusCode.Should().Be(200);
        row.RequestPath.Should().Be("/v1/payment_links");
        row.CorrelationId.Should().Be("corr-9");
        row.Message.Should().NotContain("ref-1").And.NotContain("c2VjcmV0");
    }

    [Fact]
    public async Task Outbound_ErrorStatus_IsFailed()
    {
        var (writer, rows) = Writer();

        await Client(writer.Object, _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)).GetAsync("https://localhost:7136/api/applications");

        rows.Single().Entry.Status.Should().Be("Failed");
        rows.Single().Entry.StatusCode.Should().Be(503);
        rows.Single().Entry.Source.Should().Be("Outbound localhost:7136");
    }

    [Fact]
    public async Task Outbound_NetworkFailure_IsLoggedAndRethrown()
    {
        var (writer, rows) = Writer();
        var client = Client(writer.Object, _ => throw new HttpRequestException("connection refused"));

        var act = () => client.GetAsync("https://api.razorpay.com/v1/payment_links/plink_1");

        await act.Should().ThrowAsync<HttpRequestException>();
        rows.Single().Entry.Status.Should().Be("Failed");
        rows.Single().Entry.Message.Should().Contain("connection refused");
    }

    // ---------------------------------------------------------------- error rows carry the request

    [Fact]
    public async Task ErrorRow_CarriesPathMethodCorrelationAndActualStatus()
    {
        var (writer, rows) = Writer();
        var http = new DefaultHttpContext { TraceIdentifier = "corr-2" };
        http.Request.Method = "PUT";
        http.Request.Path = "/api/patients/5";
        http.Response.StatusCode = 409;
        var service = new ErrorLoggingService(writer.Object, Options.Create(new AppLoggingOptions()), Env(),
            NullLogger<ErrorLoggingService>.Instance, new HttpContextAccessor { HttpContext = http });

        await service.LogAsync(LogCategory.Api, "Api.Middleware", "conflict");

        var row = rows.Single().Entry;
        row.RequestPath.Should().Be("/api/patients/5");
        row.HttpMethod.Should().Be("PUT");
        row.CorrelationId.Should().Be("corr-2");
        row.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task FrontendErrorRow_DoesNotBorrowTheReportingRequestsPath()
    {
        var (writer, rows) = Writer();
        var http = new DefaultHttpContext();
        http.Request.Path = "/api/logs/frontend";
        var service = new ErrorLoggingService(writer.Object, Options.Create(new AppLoggingOptions()), Env(),
            NullLogger<ErrorLoggingService>.Instance, new HttpContextAccessor { HttpContext = http });

        await service.LogAsync(LogCategory.Frontend, "Frontend.ErrorBoundary", "boom");

        rows.Single().Entry.RequestPath.Should().BeNull();
    }

    // ---------------------------------------------------------------- queue + scrubbing

    [Fact]
    public async Task QueuedWriter_ReturnsImmediately_AndDropsWhenFull()
    {
        var queue = new AppLogQueue(Options.Create(new AppLoggingOptions { QueueCapacity = 16 }));
        var writer = new QueuedAppLogWriter(queue);
        var entry = new LogEntry { Category = "Api", Source = "s", Message = "m", Environment = "e" };

        for (var i = 0; i < 16; i++)
            await writer.WriteAsync("T", entry);

        queue.TryEnqueue("T", entry).Should().BeFalse();
    }

    [Theory]
    [InlineData("DefaultEndpointsProtocol=https;AccountName=a;AccountKey=abcDEF123+/==", "abcDEF123")]
    [InlineData("Authorization: Bearer eyJhbGciOi.payload.sig", "eyJhbGciOi")]
    [InlineData("X-Razorpay-Signature: 9f8e7d", "9f8e7d")]
    [InlineData("otp=123456", "123456")]
    [InlineData("KeySecret=rzp_secret_1", "rzp_secret_1")]
    public void Scrubber_MasksSecrets(string input, string secret)
    {
        LogScrubber.Scrub(input).Should().NotContain(secret);
    }

    [Fact]
    public void Scrubber_BoundsTheLength()
    {
        LogScrubber.Scrub(new string('x', 40000))!.Length.Should().Be(30000);
    }
}
