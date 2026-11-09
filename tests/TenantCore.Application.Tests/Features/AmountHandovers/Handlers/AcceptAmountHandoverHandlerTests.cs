using FluentAssertions;
using Moq;
using TenantCore.Application.Features.AmountHandovers.Commands;
using TenantCore.Application.Features.AmountHandovers.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.AmountHandovers.Handlers;

public class AcceptAmountHandoverHandlerTests
{
    private readonly Mock<IAmountHandoverRepository> _repository = new();
    private readonly Mock<ICounterSessionRepository> _sessionRepository = new();
    private readonly Mock<IOpdPaymentRepository> _paymentRepository = new();
    private readonly Mock<IOpdParticularRepository> _particularRepository = new();
    private readonly Mock<IExpenseRecordRepository> _expenseRepository = new();

    public AcceptAmountHandoverHandlerTests()
    {
        _particularRepository.Setup(r => r.GetIndividuallyCollectedTotalsByRegistrationIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _particularRepository.Setup(r => r.GetCollectedBySessionIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        // Default: no other handovers exist for the session yet.
        _repository.Setup(r => r.GetBySessionIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private AcceptAmountHandoverHandler CreateHandler() =>
        new(_repository.Object, _sessionRepository.Object, _paymentRepository.Object, _particularRepository.Object, _expenseRepository.Object);

    [Fact]
    public async Task Handle_WhenPending_AcceptsHandover()
    {
        var appId = Guid.NewGuid();
        var handover = AmountHandover.Create(appId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Doctor", 1000, null);

        _repository.Setup(r => r.GetByIdAsync(handover.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(handover);
        _repository.Setup(r => r.Update(handover));
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var result = await handler.Handle(new AcceptAmountHandoverCommand(handover.Id, appId), CancellationToken.None);

        result.Status.Should().Be(HandoverStatus.Accepted);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ThrowsNotFoundException()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AmountHandover?)null);

        var handler = CreateHandler();
        var action = () => handler.Handle(new AcceptAmountHandoverCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenAlreadyAccepted_ThrowsInvalidOperationException()
    {
        var appId = Guid.NewGuid();
        var handover = AmountHandover.Create(appId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Doctor", 1000, null);
        handover.Accept();

        _repository.Setup(r => r.GetByIdAsync(handover.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(handover);

        var handler = CreateHandler();
        var action = () => handler.Handle(new AcceptAmountHandoverCommand(handover.Id, appId), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    // ── Session-balance guard (a refund between send and accept shrinks the till) ──

    [Fact]
    public async Task Handle_WhenAmountStillMatchesSessionBalance_AcceptsHandover()
    {
        var appId = Guid.NewGuid();
        var session = CounterSession.Create(appId, Guid.NewGuid(), DateTime.Today);
        var payment = OpdPayment.Create(appId, Guid.NewGuid(), 1400);
        payment.AcceptVisitFee(Guid.NewGuid(), session.Id);

        var handover = AmountHandover.Create(appId, session.Id, Guid.NewGuid(), Guid.NewGuid(), "Doctor", 1400, null);

        _repository.Setup(r => r.GetByIdAsync(handover.Id, It.IsAny<CancellationToken>())).ReturnsAsync(handover);
        _sessionRepository.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _paymentRepository.Setup(r => r.GetBySessionIdAsync(session.Id, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([payment]);

        var handler = CreateHandler();
        var result = await handler.Handle(new AcceptAmountHandoverCommand(handover.Id, appId), CancellationToken.None);

        result.Status.Should().Be(HandoverStatus.Accepted);
    }

    [Fact]
    public async Task Handle_WhenHandoverAmountNoLongerMatchesSessionBalance_ThrowsInvalidOperationException()
    {
        var appId = Guid.NewGuid();
        var session = CounterSession.Create(appId, Guid.NewGuid(), DateTime.Today);
        // Counter has 1400 collected; a 350 refund brought the live balance down to 1050 while
        // the handover still reads 1400.
        var payment = OpdPayment.Create(appId, Guid.NewGuid(), 1400);
        payment.AcceptVisitFee(Guid.NewGuid(), session.Id);
        payment.ApplyDiscount(350, Guid.NewGuid());
        payment.ProcessRefund(Guid.NewGuid());

        var handover = AmountHandover.Create(appId, session.Id, Guid.NewGuid(), Guid.NewGuid(), "Doctor", 1400, null);

        _repository.Setup(r => r.GetByIdAsync(handover.Id, It.IsAny<CancellationToken>())).ReturnsAsync(handover);
        _sessionRepository.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _paymentRepository.Setup(r => r.GetBySessionIdAsync(session.Id, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([payment]);

        var handler = CreateHandler();
        var action = () => handler.Handle(new AcceptAmountHandoverCommand(handover.Id, appId), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*outstanding balance*");
        handover.Status.Should().Be(HandoverStatus.Pending);
    }

    [Fact]
    public async Task Handle_WhenSessionIncludesIndividuallyCollectedItems_DoesNotDoubleCountTheirAmount()
    {
        // DEF-E2E-06 regression: CollectedAmount now folds in individually collected items
        // (RecordItemCollection). The guard backs the item amount out of the payment's base and
        // adds it back via items collected in this same session, landing on the same total.
        var appId = Guid.NewGuid();
        var session = CounterSession.Create(appId, Guid.NewGuid(), DateTime.Today);
        var payment = OpdPayment.Create(appId, Guid.NewGuid(), 200);
        payment.UpdateParticularsTotal(500);
        payment.AcceptVisitFee(Guid.NewGuid(), session.Id);
        payment.RecordItemCollection(500, Guid.NewGuid(), session.Id);
        var item = OpdParticular.Create(appId, payment.OpdRegistrationId, Guid.NewGuid(), "USG", 500);
        item.Collect(Guid.NewGuid(), session.Id);

        var handover = AmountHandover.Create(appId, session.Id, Guid.NewGuid(), Guid.NewGuid(), "Doctor", 700, null);

        _repository.Setup(r => r.GetByIdAsync(handover.Id, It.IsAny<CancellationToken>())).ReturnsAsync(handover);
        _sessionRepository.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _paymentRepository.Setup(r => r.GetBySessionIdAsync(session.Id, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([payment]);
        _particularRepository.Setup(r => r.GetIndividuallyCollectedTotalsByRegistrationIdsAsync(
                It.Is<IEnumerable<Guid>>(ids => ids.Contains(payment.OpdRegistrationId)), appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, decimal> { [payment.OpdRegistrationId] = 500m });
        _particularRepository.Setup(r => r.GetCollectedBySessionIdAsync(session.Id, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        var handler = CreateHandler();
        var result = await handler.Handle(new AcceptAmountHandoverCommand(handover.Id, appId), CancellationToken.None);

        result.Status.Should().Be(HandoverStatus.Accepted);
    }

    [Fact]
    public async Task Handle_WhenSessionHasAnItemCollectedForAForeignPayment_CreditsItsAmount()
    {
        // DEF-E2E-08: an item billed under session A's payment but individually collected in THIS
        // session isn't inside this session's payments.Sum(CollectedAmount) — the guard must credit
        // it via items collected in this session, or a correct handover gets rejected as a mismatch.
        var appId = Guid.NewGuid();
        var session = CounterSession.Create(appId, Guid.NewGuid(), DateTime.Today);
        var foreignRegistrationId = Guid.NewGuid();
        var item = OpdParticular.Create(appId, foreignRegistrationId, Guid.NewGuid(), "Nursing", 400);
        item.Collect(Guid.NewGuid(), session.Id);
        var handover = AmountHandover.Create(appId, session.Id, Guid.NewGuid(), Guid.NewGuid(), "Doctor", 400, null);

        _repository.Setup(r => r.GetByIdAsync(handover.Id, It.IsAny<CancellationToken>())).ReturnsAsync(handover);
        _sessionRepository.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _paymentRepository.Setup(r => r.GetBySessionIdAsync(session.Id, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _particularRepository.Setup(r => r.GetCollectedBySessionIdAsync(session.Id, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        var handler = CreateHandler();
        var result = await handler.Handle(new AcceptAmountHandoverCommand(handover.Id, appId), CancellationToken.None);

        result.Status.Should().Be(HandoverStatus.Accepted);
    }

    // ── DEF-MR-02 (QA re-test): a second handover for a session that already had money accepted ──

    [Fact]
    public async Task Handle_WhenPartOfTheBalanceAlreadyAccepted_AcceptsOnlyTheOutstandingAmount()
    {
        // Session collected 3,900, already handed over and accepted. A further 100 was collected
        // afterwards (session still open) and a new handover for exactly that 100 is pending.
        var appId = Guid.NewGuid();
        var session = CounterSession.Create(appId, Guid.NewGuid(), DateTime.Today);
        var payment = OpdPayment.Create(appId, Guid.NewGuid(), 4000);
        payment.AcceptVisitFee(Guid.NewGuid(), session.Id);

        var alreadyAccepted = AmountHandover.Create(appId, session.Id, Guid.NewGuid(), Guid.NewGuid(), "Doctor", 3900, null);
        alreadyAccepted.Accept();
        var newHandover = AmountHandover.Create(appId, session.Id, Guid.NewGuid(), Guid.NewGuid(), "Doctor", 100, null);

        _repository.Setup(r => r.GetByIdAsync(newHandover.Id, It.IsAny<CancellationToken>())).ReturnsAsync(newHandover);
        _repository.Setup(r => r.GetBySessionIdAsync(session.Id, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([alreadyAccepted]);
        _sessionRepository.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _paymentRepository.Setup(r => r.GetBySessionIdAsync(session.Id, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([payment]);

        var handler = CreateHandler();
        var result = await handler.Handle(new AcceptAmountHandoverCommand(newHandover.Id, appId), CancellationToken.None);

        result.Status.Should().Be(HandoverStatus.Accepted);
    }

    [Fact]
    public async Task Handle_WhenHandoverAmountIgnoresAlreadyAcceptedPortion_ThrowsInvalidOperationException()
    {
        // Same setup as above, but the new handover was (wrongly) raised for the FULL cumulative
        // total (4,000) instead of just the 100 still outstanding — must be rejected, not double
        // counted on top of the 3,900 already accepted.
        var appId = Guid.NewGuid();
        var session = CounterSession.Create(appId, Guid.NewGuid(), DateTime.Today);
        var payment = OpdPayment.Create(appId, Guid.NewGuid(), 4000);
        payment.AcceptVisitFee(Guid.NewGuid(), session.Id);

        var alreadyAccepted = AmountHandover.Create(appId, session.Id, Guid.NewGuid(), Guid.NewGuid(), "Doctor", 3900, null);
        alreadyAccepted.Accept();
        var newHandover = AmountHandover.Create(appId, session.Id, Guid.NewGuid(), Guid.NewGuid(), "Doctor", 4000, null);

        _repository.Setup(r => r.GetByIdAsync(newHandover.Id, It.IsAny<CancellationToken>())).ReturnsAsync(newHandover);
        _repository.Setup(r => r.GetBySessionIdAsync(session.Id, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([alreadyAccepted]);
        _sessionRepository.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _paymentRepository.Setup(r => r.GetBySessionIdAsync(session.Id, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([payment]);

        var handler = CreateHandler();
        var action = () => handler.Handle(new AcceptAmountHandoverCommand(newHandover.Id, appId), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*outstanding balance*");
        newHandover.Status.Should().Be(HandoverStatus.Pending);
    }
}
