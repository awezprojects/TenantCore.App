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
    private readonly Mock<IOpdPaymentRepository> _paymentRepo = new();
    private readonly Mock<IOpdParticularRepository> _particularRepo = new();
    private readonly Mock<IExpenseRecordRepository> _expenseRepo = new();

    public CreateAmountHandoverHandlerTests()
    {
        // Default: an empty session (no payments/items/expenses) — net balance 0.
        _paymentRepo.Setup(r => r.GetBySessionIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _particularRepo.Setup(r => r.GetIndividuallyCollectedTotalsByRegistrationIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _particularRepo.Setup(r => r.GetCollectedBySessionIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _expenseRepo.Setup(r => r.GetBySessionIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        // Default: no other handovers exist for the session yet.
        _handoverRepo.Setup(r => r.GetBySessionIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private CreateAmountHandoverHandler CreateHandler() => new(
        _handoverRepo.Object, _sessionRepo.Object, _paymentRepo.Object, _particularRepo.Object, _expenseRepo.Object);

    [Fact]
    public async Task Handle_WhenAmountMatchesSessionBalance_CreatesHandoverAndReturnsGuid()
    {
        var appId = Guid.NewGuid();
        var session = CounterSession.Create(appId, Guid.NewGuid(), DateTime.Today);

        _sessionRepo.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _handoverRepo.Setup(r => r.AddAsync(It.IsAny<AmountHandover>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _handoverRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // No payments/expenses set up above, so the session's net balance is 0 — the handover
        // amount must match it.
        var command = new CreateAmountHandoverCommand(
            new CreateAmountHandoverRequest
            {
                CounterSessionId = session.Id,
                HandedOverToUserId = Guid.NewGuid(),
                Amount = 0,
                Notes = "Daily collection"
            },
            Guid.NewGuid(), appId);

        var handler = CreateHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        _handoverRepo.Verify(r => r.AddAsync(It.IsAny<AmountHandover>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // DEF-MR-02: a handover for less than the full session balance (e.g. "my own share" of a
    // clinic-wide session in a multi-reception clinic) used to be accepted here with a 201 and
    // only fail later, at Accept, with a 409 the sender never sees.
    [Fact]
    public async Task Handle_WhenAmountDoesNotMatchSessionBalance_ThrowsInvalidOperationException()
    {
        var appId = Guid.NewGuid();
        var session = CounterSession.Create(appId, Guid.NewGuid(), DateTime.Today);

        _sessionRepo.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var command = new CreateAmountHandoverCommand(
            new CreateAmountHandoverRequest
            {
                CounterSessionId = session.Id,
                HandedOverToUserId = Guid.NewGuid(),
                Amount = 1850 // session balance is 0 (no payments set up)
            },
            Guid.NewGuid(), appId);

        var handler = CreateHandler();
        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        _handoverRepo.Verify(r => r.AddAsync(It.IsAny<AmountHandover>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // DEF-MR-02 (QA re-test): a session that collected more cash after an earlier handover was
    // already Accepted used to let a second handover be raised for the full cumulative balance
    // again, double-counting the cash already handed over.
    [Fact]
    public async Task Handle_WhenPartOfTheBalanceAlreadyAccepted_RequiresOnlyTheOutstandingAmount()
    {
        var appId = Guid.NewGuid();
        var session = CounterSession.Create(appId, Guid.NewGuid(), DateTime.Today);
        var payment = OpdPayment.Create(appId, Guid.NewGuid(), 4000);
        payment.AcceptVisitFee(Guid.NewGuid(), session.Id);

        var alreadyAccepted = AmountHandover.Create(appId, session.Id, Guid.NewGuid(), Guid.NewGuid(), "Doctor", 3900, null);
        alreadyAccepted.Accept();

        _sessionRepo.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _paymentRepo.Setup(r => r.GetBySessionIdAsync(session.Id, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([payment]);
        _handoverRepo.Setup(r => r.GetBySessionIdAsync(session.Id, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([alreadyAccepted]);
        _handoverRepo.Setup(r => r.AddAsync(It.IsAny<AmountHandover>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _handoverRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Raising the new handover for the full cumulative 4,000 (ignoring the 3,900 already
        // accepted) must be rejected.
        var fullAmountCommand = new CreateAmountHandoverCommand(
            new CreateAmountHandoverRequest { CounterSessionId = session.Id, HandedOverToUserId = Guid.NewGuid(), Amount = 4000 },
            Guid.NewGuid(), appId);
        var action = () => CreateHandler().Handle(fullAmountCommand, CancellationToken.None);
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*outstanding balance*");

        // Raising it for just the outstanding 100 succeeds.
        var outstandingCommand = new CreateAmountHandoverCommand(
            new CreateAmountHandoverRequest { CounterSessionId = session.Id, HandedOverToUserId = Guid.NewGuid(), Amount = 100 },
            Guid.NewGuid(), appId);
        var result = await CreateHandler().Handle(outstandingCommand, CancellationToken.None);
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WhenAPendingHandoverAlreadyExistsForTheSession_ThrowsInvalidOperationException()
    {
        var appId = Guid.NewGuid();
        var session = CounterSession.Create(appId, Guid.NewGuid(), DateTime.Today);
        var pending = AmountHandover.Create(appId, session.Id, Guid.NewGuid(), Guid.NewGuid(), "Doctor", 500, null);

        _sessionRepo.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _handoverRepo.Setup(r => r.GetBySessionIdAsync(session.Id, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([pending]);

        var command = new CreateAmountHandoverCommand(
            new CreateAmountHandoverRequest { CounterSessionId = session.Id, HandedOverToUserId = Guid.NewGuid(), Amount = 500 },
            Guid.NewGuid(), appId);

        var action = () => CreateHandler().Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already pending*");
        _handoverRepo.Verify(r => r.AddAsync(It.IsAny<AmountHandover>(), It.IsAny<CancellationToken>()), Times.Never);
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

        var handler = CreateHandler();
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

        var handler = CreateHandler();
        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }
}
