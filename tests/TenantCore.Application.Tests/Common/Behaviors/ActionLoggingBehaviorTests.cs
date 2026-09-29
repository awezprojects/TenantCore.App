using FluentAssertions;
using MediatR;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Common.Behaviors;
using TenantCore.Application.Services;

namespace TenantCore.Application.Tests.Common.Behaviors;

public class ActionLoggingBehaviorTests
{
    [Fact]
    public async Task Handle_RequestNotImplementingIBusinessAction_NeverCallsActionLogger()
    {
        var actionLogger = new Mock<IActionLogger>();
        var currentUser = new Mock<ICurrentUserContext>();
        var behavior = new ActionLoggingBehavior<PlainRequest, string>(actionLogger.Object, currentUser.Object);

        var result = await behavior.Handle(new PlainRequest(), () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
        actionLogger.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_BusinessActionSucceeds_LogsStartedThenCompletedWithSameCorrelationId()
    {
        var actionLogger = new Mock<IActionLogger>();
        var currentUser = new Mock<ICurrentUserContext>();
        currentUser.SetupGet(c => c.UserId).Returns(Guid.NewGuid());
        var behavior = new ActionLoggingBehavior<AuditedRequest, string>(actionLogger.Object, currentUser.Object);

        var appId = Guid.NewGuid();
        var request = new AuditedRequest(appId);

        Guid? startedCorrelationId = null;
        actionLogger
            .Setup(l => l.LogStartedAsync(It.IsAny<Guid>(), "Test Action", nameof(AuditedRequest), appId, currentUser.Object.UserId, It.IsAny<CancellationToken>()))
            .Callback<Guid, string, string, Guid?, Guid?, CancellationToken>((cid, _, _, _, _, _) => startedCorrelationId = cid)
            .Returns(Task.CompletedTask);

        var result = await behavior.Handle(request, () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
        startedCorrelationId.Should().NotBeNull();
        actionLogger.Verify(l => l.LogCompletedAsync(
            startedCorrelationId!.Value, "Test Action", nameof(AuditedRequest), appId, currentUser.Object.UserId, It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Once);
        actionLogger.Verify(l => l.LogFailedAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_HandlerThrows_LogsFailedAndRethrowsOriginalException()
    {
        var actionLogger = new Mock<IActionLogger>();
        var currentUser = new Mock<ICurrentUserContext>();
        var behavior = new ActionLoggingBehavior<AuditedRequest, string>(actionLogger.Object, currentUser.Object);

        var request = new AuditedRequest(Guid.NewGuid());
        var thrown = new InvalidOperationException("boom");

        Func<Task> act = () => behavior.Handle(request, () => throw thrown, CancellationToken.None);

        var caught = await act.Should().ThrowAsync<InvalidOperationException>();
        caught.Which.Should().BeSameAs(thrown);

        actionLogger.Verify(l => l.LogFailedAsync(
            It.IsAny<Guid>(), "Test Action", nameof(AuditedRequest), request.ApplicationId, It.IsAny<Guid?>(), It.IsAny<long>(), "boom", It.IsAny<CancellationToken>()),
            Times.Once);
        actionLogger.Verify(l => l.LogCompletedAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_RequestWithoutApplicationIdProperty_PassesNullApplicationId()
    {
        var actionLogger = new Mock<IActionLogger>();
        var currentUser = new Mock<ICurrentUserContext>();
        var behavior = new ActionLoggingBehavior<AuditedRequestWithoutTenant, string>(actionLogger.Object, currentUser.Object);

        await behavior.Handle(new AuditedRequestWithoutTenant(), () => Task.FromResult("ok"), CancellationToken.None);

        actionLogger.Verify(l => l.LogStartedAsync(
            It.IsAny<Guid>(), "Global Action", nameof(AuditedRequestWithoutTenant), null, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    public sealed record PlainRequest : IRequest<string>;

    public sealed record AuditedRequest(Guid ApplicationId) : IRequest<string>, IBusinessAction
    {
        public string ActionName => "Test Action";
    }

    public sealed record AuditedRequestWithoutTenant : IRequest<string>, IBusinessAction
    {
        public string ActionName => "Global Action";
    }
}

/// <summary>ADR-011: action logging is ON by default for every *Command.</summary>
public class ActionLoggingBehaviorDefaultOnTests
{
    private readonly Mock<IActionLogger> _actionLogger = new();
    private readonly Mock<ICurrentUserContext> _currentUser = new();

    [Fact]
    public async Task CommandByConvention_IsLoggedWithoutIBusinessAction_UsingAHumanizedName()
    {
        var appId = Guid.NewGuid();
        var behavior = new ActionLoggingBehavior<CreateWidgetCommand, string>(_actionLogger.Object, _currentUser.Object);

        await behavior.Handle(new CreateWidgetCommand(appId), () => Task.FromResult("ok"), CancellationToken.None);

        _actionLogger.Verify(l => l.LogStartedAsync(It.IsAny<Guid>(), "Create Widget", nameof(CreateWidgetCommand), appId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
        _actionLogger.Verify(l => l.LogCompletedAsync(It.IsAny<Guid>(), "Create Widget", nameof(CreateWidgetCommand), appId, It.IsAny<Guid?>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SkipActionLog_OptsTheCommandOut()
    {
        var behavior = new ActionLoggingBehavior<SkippedCommand, string>(_actionLogger.Object, _currentUser.Object);

        await behavior.Handle(new SkippedCommand(), () => Task.FromResult("ok"), CancellationToken.None);

        _actionLogger.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ActionLogContext_IsAppendedToTheRequestType()
    {
        var behavior = new ActionLoggingBehavior<ContextCommand, string>(_actionLogger.Object, _currentUser.Object);

        await behavior.Handle(new ContextCommand(), () => Task.FromResult("ok"), CancellationToken.None);

        _actionLogger.Verify(l => l.LogStartedAsync(It.IsAny<Guid>(), "Context", "ContextCommand | eventId=evt_1", null, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NullableApplicationId_IsPickedUp()
    {
        var appId = Guid.NewGuid();
        var behavior = new ActionLoggingBehavior<NullableTenantCommand, string>(_actionLogger.Object, _currentUser.Object);

        await behavior.Handle(new NullableTenantCommand(appId), () => Task.FromResult("ok"), CancellationToken.None);

        _actionLogger.Verify(l => l.LogStartedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), appId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("CreatePatientCommand", "Create Patient")]
    [InlineData("AcceptOpdPaymentFullCommand", "Accept Opd Payment Full")]
    [InlineData("SetObstetricEddByUsgCommand", "Set Obstetric Edd By Usg")]
    [InlineData("RecordHTTPCallCommand", "Record HTTP Call")]
    [InlineData("Command", "Command")]
    public void Humanize_SplitsPascalCase_AndDropsTheCommandSuffix(string type, string expected)
    {
        ActionLoggingBehavior<CreateWidgetCommand, string>.Humanize(type).Should().Be(expected);
    }

    [Fact]
    public void RealCommands_AreLoggedByDefault_AndTheFrontendLogCommandIsOptedOut()
    {
        // Every command type in the Application assembly is action-logged unless it explicitly opts out.
        var commandTypes = typeof(ActionLoggingBehavior<,>).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Command", StringComparison.Ordinal))
            .ToList();

        commandTypes.Should().HaveCountGreaterThan(50);
        commandTypes.Where(t => typeof(ISkipActionLog).IsAssignableFrom(t)).Select(t => t.Name)
            .Should().BeEquivalentTo(["LogFrontendErrorCommand"], "opting out of action logging needs a documented reason (ADR-011)");
    }

    public sealed record CreateWidgetCommand(Guid ApplicationId) : IRequest<string>;
    public sealed record SkippedCommand : IRequest<string>, ISkipActionLog;
    public sealed record NullableTenantCommand(Guid? ApplicationId) : IRequest<string>;

    public sealed record ContextCommand : IRequest<string>, IActionLogContext
    {
        public string? ActionLogContext => "eventId=evt_1";
    }
}
