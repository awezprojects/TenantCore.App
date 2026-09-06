using FluentAssertions;
using Moq;
using TenantCore.Application.Features.CounterSessions.Commands;
using TenantCore.Application.Features.CounterSessions.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Tests.Features.CounterSessions.Handlers;

public class CloseCounterSessionHandlerTests
{
    private readonly Mock<ICounterSessionRepository> _sessionRepo = new();
    private readonly Mock<IOpdPaymentRepository> _paymentRepo = new();
    private readonly Mock<IOpdParticularRepository> _particularRepo = new();
    private readonly Mock<IExpenseRecordRepository> _expenseRepo = new();
    private readonly Mock<IAmountHandoverRepository> _handoverRepo = new();

    public CloseCounterSessionHandlerTests()
    {
        // Default: no payments, particulars, expenses, or handovers.
        _paymentRepo.Setup(r => r.GetBySessionIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _particularRepo.Setup(r => r.GetCollectedBySessionIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _expenseRepo.Setup(r => r.GetBySessionIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _handoverRepo.Setup(r => r.GetBySessionIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    [Fact]
    public async Task Handle_WhenSessionExists_ClosesSession()
    {
        var appId = Guid.NewGuid();
        var session = CounterSession.Create(appId, Guid.NewGuid(), DateTime.Today);

        // Closing a session requires the cash to have already been handed over and accepted.
        var handover = AmountHandover.Create(appId, session.Id, Guid.NewGuid(), Guid.NewGuid(), "Clinic Admin", 0m, null);
        handover.Accept();

        _sessionRepo.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _sessionRepo.Setup(r => r.Update(session));
        _sessionRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _handoverRepo.Setup(r => r.GetBySessionIdAsync(session.Id, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([handover]);

        var handler = new CloseCounterSessionHandler(_sessionRepo.Object, _paymentRepo.Object, _particularRepo.Object, _expenseRepo.Object, _handoverRepo.Object);
        var result = await handler.Handle(new CloseCounterSessionCommand(session.Id, appId), CancellationToken.None);

        result.Status.Should().Be(CounterSessionStatus.Closed);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ThrowsNotFoundException()
    {
        _sessionRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CounterSession?)null);

        var handler = new CloseCounterSessionHandler(_sessionRepo.Object, _paymentRepo.Object, _particularRepo.Object, _expenseRepo.Object, _handoverRepo.Object);
        var action = () => handler.Handle(new CloseCounterSessionCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenCrossTenant_ThrowsNotFoundException()
    {
        var session = CounterSession.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.Today);
        _sessionRepo.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var handler = new CloseCounterSessionHandler(_sessionRepo.Object, _paymentRepo.Object, _particularRepo.Object, _expenseRepo.Object, _handoverRepo.Object);
        var action = () => handler.Handle(new CloseCounterSessionCommand(session.Id, Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }
}
