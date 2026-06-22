using FluentAssertions;
using Moq;
using TenantCore.Application.Features.CounterSessions.Handlers;
using TenantCore.Application.Features.CounterSessions.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.CounterSessions.Handlers;

public class GetActiveCounterSessionHandlerTests
{
    private readonly Mock<ICounterSessionRepository> _repository = new();
    private readonly Mock<IOpdPaymentRepository> _paymentRepo = new();
    private readonly Mock<IOpdParticularRepository> _particularRepo = new();
    private readonly Mock<IExpenseRecordRepository> _expenseRepo = new();

    public GetActiveCounterSessionHandlerTests()
    {
        _paymentRepo.Setup(r => r.GetBySessionIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _particularRepo.Setup(r => r.GetCollectedBySessionIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _expenseRepo.Setup(r => r.GetBySessionIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    [Fact]
    public async Task Handle_WhenActiveSessionExists_ReturnsDto()
    {
        var appId = Guid.NewGuid();
        var session = CounterSession.Create(appId, Guid.NewGuid(), DateTime.Today);

        _repository.Setup(r => r.GetActiveSessionAsync(appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var handler = new GetActiveCounterSessionHandler(_repository.Object, _paymentRepo.Object, _particularRepo.Object, _expenseRepo.Object);
        var result = await handler.Handle(new GetActiveCounterSessionQuery(appId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(session.Id);
    }

    [Fact]
    public async Task Handle_WhenNoActiveSession_ReturnsNull()
    {
        var appId = Guid.NewGuid();
        _repository.Setup(r => r.GetActiveSessionAsync(appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CounterSession?)null);

        var handler = new GetActiveCounterSessionHandler(_repository.Object, _paymentRepo.Object, _particularRepo.Object, _expenseRepo.Object);
        var result = await handler.Handle(new GetActiveCounterSessionQuery(appId), CancellationToken.None);

        result.Should().BeNull();
    }
}
