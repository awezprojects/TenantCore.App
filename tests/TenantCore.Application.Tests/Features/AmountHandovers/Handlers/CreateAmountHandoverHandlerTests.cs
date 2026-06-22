using FluentAssertions;
using Moq;
using TenantCore.Application.Features.AmountHandovers.Commands;
using TenantCore.Application.Features.AmountHandovers.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Tests.Features.AmountHandovers.Handlers;

public class CreateAmountHandoverHandlerTests
{
    private readonly Mock<IAmountHandoverRepository> _handoverRepo = new();
    private readonly Mock<ICounterSessionRepository> _sessionRepo = new();

    [Fact]
    public async Task Handle_WhenSessionExists_CreatesHandoverAndReturnsGuid()
    {
        var appId = Guid.NewGuid();
        var session = CounterSession.Create(appId, Guid.NewGuid(), DateTime.Today);

        _sessionRepo.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _handoverRepo.Setup(r => r.AddAsync(It.IsAny<AmountHandover>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _handoverRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CreateAmountHandoverCommand(
            new CreateAmountHandoverRequest
            {
                CounterSessionId = session.Id,
                HandedOverToUserId = Guid.NewGuid(),
                Amount = 1000,
                Notes = "Daily collection"
            },
            Guid.NewGuid(), appId);

        var handler = new CreateAmountHandoverHandler(_handoverRepo.Object, _sessionRepo.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        _handoverRepo.Verify(r => r.AddAsync(It.IsAny<AmountHandover>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSessionNotFound_ThrowsNotFoundException()
    {
        var appId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        _sessionRepo.Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CounterSession?)null);

        var command = new CreateAmountHandoverCommand(
            new CreateAmountHandoverRequest
            {
                CounterSessionId = sessionId,
                HandedOverToUserId = Guid.NewGuid(),
                Amount = 1000
            },
            Guid.NewGuid(), appId);

        var handler = new CreateAmountHandoverHandler(_handoverRepo.Object, _sessionRepo.Object);
        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenSessionBelongsToDifferentTenant_ThrowsNotFoundException()
    {
        var session = CounterSession.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.Today);

        _sessionRepo.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var command = new CreateAmountHandoverCommand(
            new CreateAmountHandoverRequest
            {
                CounterSessionId = session.Id,
                HandedOverToUserId = Guid.NewGuid(),
                Amount = 1000
            },
            Guid.NewGuid(), Guid.NewGuid());

        var handler = new CreateAmountHandoverHandler(_handoverRepo.Object, _sessionRepo.Object);
        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }
}
