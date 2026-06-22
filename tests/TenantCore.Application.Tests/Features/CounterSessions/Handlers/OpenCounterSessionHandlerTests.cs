using FluentAssertions;
using Moq;
using TenantCore.Application.Features.CounterSessions.Commands;
using TenantCore.Application.Features.CounterSessions.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Tests.Features.CounterSessions.Handlers;

public class OpenCounterSessionHandlerTests
{
    private readonly Mock<ICounterSessionRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenNoActiveSession_CreatesSessionAndReturnsGuid()
    {
        var appId = Guid.NewGuid();
        var openedBy = Guid.NewGuid();

        _repository.Setup(r => r.GetActiveSessionAsync(appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CounterSession?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<CounterSession>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new OpenCounterSessionCommand(
            new OpenCounterSessionRequest { SessionDate = DateTime.Today }, openedBy, appId);

        var handler = new OpenCounterSessionHandler(_repository.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        _repository.Verify(r => r.AddAsync(It.IsAny<CounterSession>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenActiveSessionExists_ThrowsInvalidOperationException()
    {
        var appId = Guid.NewGuid();
        var existing = CounterSession.Create(appId, Guid.NewGuid(), DateTime.Today);

        _repository.Setup(r => r.GetActiveSessionAsync(appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var command = new OpenCounterSessionCommand(
            new OpenCounterSessionRequest { SessionDate = DateTime.Today }, Guid.NewGuid(), appId);

        var handler = new OpenCounterSessionHandler(_repository.Object);
        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        _repository.Verify(r => r.AddAsync(It.IsAny<CounterSession>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
