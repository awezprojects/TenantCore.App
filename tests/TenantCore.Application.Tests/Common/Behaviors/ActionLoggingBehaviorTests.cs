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
