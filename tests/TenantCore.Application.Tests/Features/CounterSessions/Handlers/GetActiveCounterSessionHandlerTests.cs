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
        _particularRepo.Setup(r => r.GetIndividuallyCollectedTotalsByRegistrationIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
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

    [Fact]
    public async Task Handle_WhenSessionHasAnItemCollectedForAForeignPayment_CreditsItsAmount()
    {
        // DEF-E2E-08: an item billed under another session's payment but individually collected
        // in THIS session isn't inside payments.Sum(CollectedAmount) for this session.
        var appId = Guid.NewGuid();
        var session = CounterSession.Create(appId, Guid.NewGuid(), DateTime.Today);
        var item = OpdParticular.Create(appId, Guid.NewGuid(), Guid.NewGuid(), "Nursing", 400);
        item.Collect(Guid.NewGuid(), session.Id);

        _repository.Setup(r => r.GetActiveSessionAsync(appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _particularRepo.Setup(r => r.GetCollectedBySessionIdAsync(session.Id, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        var handler = new GetActiveCounterSessionHandler(_repository.Object, _paymentRepo.Object, _particularRepo.Object, _expenseRepo.Object);
        var result = await handler.Handle(new GetActiveCounterSessionQuery(appId), CancellationToken.None);

        result!.TotalCollected.Should().Be(400m);
    }
}
