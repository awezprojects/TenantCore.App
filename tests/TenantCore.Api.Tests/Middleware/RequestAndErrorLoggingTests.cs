using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Serilog.Events;
using Serilog.Parsing;
using TenantCore.Api.Logging;
using TenantCore.Api.Middleware;
using TenantCore.Application.Services;
using TenantCore.Domain.Exceptions;
using TenantCore.Logging;
using TenantCore.Shared.Enums;

namespace TenantCore.Api.Tests.Middleware;

/// <summary>ADR-011 request / error logging.</summary>
public class RequestAndErrorLoggingTests
{
    private static readonly AppLoggingOptions Defaults = new();

    private static IHostEnvironment Env()
    {
        var env = new Mock<IHostEnvironment>();
        env.SetupGet(e => e.EnvironmentName).Returns("Testing");
        return env.Object;
    }

    // ---------------------------------------------------------------- request log policy

    [Theory]
    [InlineData("GET", "/api/patients", 200, 50, false)]      // fast successful read: not logged
    [InlineData("GET", "/api/patients", 404, 5, true)]        // handled 4xx
    [InlineData("GET", "/api/patients", 500, 5, true)]
    [InlineData("GET", "/api/reports", 200, 2500, true)]      // slow
    [InlineData("POST", "/api/patients", 201, 30, true)]      // data change
    [InlineData("DELETE", "/api/beds/1", 204, 30, true)]
    [InlineData("POST", "/api/logs/frontend", 200, 10, false)] // the error-report endpoint itself
    [InlineData("POST", "/api/logs/frontend", 400, 10, true)]
    public void ShouldLog_FailedSlowAndMutatingRequests(string method, string path, int status, long ms, bool expected)
    {
        ApiRequestLoggingMiddleware.ShouldLog(method, path, status, ms, Defaults).Should().Be(expected);
    }

    [Fact]
    public void ShouldLog_MutationsCanBeTurnedOff()
    {
        ApiRequestLoggingMiddleware.ShouldLog("POST", "/api/x", 200, 5, new AppLoggingOptions { LogMutatingRequests = false }).Should().BeFalse();
    }

    [Fact]
    public async Task RequestRow_CarriesMethodPathStatusDurationTenantUserAndCorrelation_NoQueryString()
    {
        var writer = new Mock<IAppLogWriter>();
        LogEntry? row = null;
        string? table = null;
        writer.Setup(w => w.WriteAsync(It.IsAny<string>(), It.IsAny<LogEntry>(), It.IsAny<CancellationToken>()))
            .Callback<string, LogEntry, CancellationToken>((t, e, _) => { table = t; row = e; })
            .Returns(Task.CompletedTask);

        var appId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/patients";
        context.Request.QueryString = new QueryString("?name=Asha");
        context.TraceIdentifier = "corr-1";
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("nameid", userId)], "test"));

        var middleware = new ApiRequestLoggingMiddleware(ctx =>
        {
            ctx.Items[ClinicContextMiddleware.ContextKey] = appId;
            ctx.Response.StatusCode = 201;
            return Task.CompletedTask;
        }, writer.Object, Options.Create(Defaults), Env());

        await middleware.InvokeAsync(context);

        table.Should().Be("ApiRequestLogs");
        row!.Category.Should().Be("Request");
        row.HttpMethod.Should().Be("POST");
        row.RequestPath.Should().Be("/api/patients");
        row.StatusCode.Should().Be(201);
        row.DurationMs.Should().NotBeNull();
        row.ApplicationId.Should().Be(appId.ToString());
        row.UserId.Should().Be(userId);
        row.CorrelationId.Should().Be("corr-1");
        row.Message.Should().NotContain("Asha");
        (row.AdditionalContext ?? string.Empty).Should().NotContain("Asha");
    }

    [Fact]
    public async Task NonApiPaths_AreNeverLogged()
    {
        var writer = new Mock<IAppLogWriter>();
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/_framework/blazor.webassembly.js";

        await new ApiRequestLoggingMiddleware(_ => Task.CompletedTask, writer.Object, Options.Create(Defaults), Env()).InvokeAsync(context);

        writer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RequestLogging_NeverBreaksTheRequest_WhenTheWriterThrows()
    {
        var writer = new Mock<IAppLogWriter>();
        writer.Setup(w => w.WriteAsync(It.IsAny<string>(), It.IsAny<LogEntry>(), It.IsAny<CancellationToken>())).Throws(new InvalidOperationException("store down"));
        var context = new DefaultHttpContext();
        context.Request.Method = "DELETE";
        context.Request.Path = "/api/beds/1";

        var act = () => new ApiRequestLoggingMiddleware(_ => Task.CompletedTask, writer.Object, Options.Create(Defaults), Env()).InvokeAsync(context);

        await act.Should().NotThrowAsync();
    }

    // ---------------------------------------------------------------- exception middleware: actual status before logging

    [Fact]
    public async Task ExceptionMiddleware_SetsTheRealStatusBeforeWritingTheErrorRow()
    {
        var errorLogger = new Mock<IErrorLogger>();
        int? statusAtLogTime = null;
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        errorLogger.Setup(l => l.LogAsync(It.IsAny<LogCategory>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback(() => statusAtLogTime = context.Response.StatusCode)
            .Returns(Task.CompletedTask);

        await new ExceptionHandlingMiddleware(_ => throw new NotFoundException("Patient", Guid.NewGuid()), Mock.Of<ILogger<ExceptionHandlingMiddleware>>())
            .InvokeAsync(context, errorLogger.Object);

        statusAtLogTime.Should().Be(404);
    }

    // ---------------------------------------------------------------- Serilog → ApiErrorLogs sink

    private static LogEvent Event(LogEventLevel level, string source, Exception? ex = null) =>
        new(DateTimeOffset.UtcNow, level, ex, new MessageTemplateParser().Parse("Sweep failed for {Count} items"),
            [new LogEventProperty("SourceContext", new ScalarValue(source)), new LogEventProperty("Count", new ScalarValue(3))]);

    private static (TableStorageErrorSink Sink, List<(string Table, LogEntry Entry)> Rows) Sink(string minimumLevel = "Error")
    {
        var rows = new List<(string, LogEntry)>();
        var writer = new Mock<IAppLogWriter>();
        writer.Setup(w => w.WriteAsync(It.IsAny<string>(), It.IsAny<LogEntry>(), It.IsAny<CancellationToken>()))
            .Callback<string, LogEntry, CancellationToken>((t, e, _) => rows.Add((t, e)))
            .Returns(Task.CompletedTask);
        var sink = new TableStorageErrorSink(writer.Object, Options.Create(new AppLoggingOptions { MinimumLevel = minimumLevel }), Env(), new HttpContextAccessor());
        return (sink, rows);
    }

    [Fact]
    public void Sink_CopiesBackgroundErrors_ToApiErrorLogs()
    {
        var (sink, rows) = Sink();

        sink.Emit(Event(LogEventLevel.Error, "TenantCore.Infrastructure.BackgroundJobs.PaymentReconciliationService", new TimeoutException("razorpay slow")));

        var (table, entry) = rows.Should().ContainSingle().Subject;
        table.Should().Be("ApiErrorLogs");
        entry.Category.Should().Be("Server");
        entry.Source.Should().Be("TenantCore.Infrastructure.BackgroundJobs.PaymentReconciliationService");
        entry.Message.Should().Be("Sweep failed for 3 items");
        entry.ExceptionType.Should().Be("System.TimeoutException");
        entry.Status.Should().Be("Error");
    }

    [Theory]
    [InlineData("TenantCore.Api.Middleware.ExceptionHandlingMiddleware")]
    [InlineData("Serilog.AspNetCore.RequestLoggingMiddleware")]
    [InlineData("TenantCore.Logging.AppLogQueueService")]
    public void Sink_SkipsSourcesAlreadyLoggedElsewhere_AndItsOwnPipeline(string source)
    {
        var (sink, rows) = Sink();

        sink.Emit(Event(LogEventLevel.Error, source));

        rows.Should().BeEmpty();
    }

    [Fact]
    public void Sink_RespectsTheMinimumLevel()
    {
        var (sink, rows) = Sink();
        sink.Emit(Event(LogEventLevel.Warning, "X"));
        rows.Should().BeEmpty();

        var (warnSink, warnRows) = Sink("Warning");
        warnSink.Emit(Event(LogEventLevel.Warning, "X"));
        warnRows.Should().ContainSingle();
    }

    [Fact]
    public void Sink_MapsFatalToCritical()
    {
        var (sink, rows) = Sink();
        sink.Emit(Event(LogEventLevel.Fatal, "Host"));
        rows.Single().Entry.Status.Should().Be("Critical");
    }
}
