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

    private AcceptAmountHandoverHandler CreateHandler() =>
        new(_repository.Object, _sessionRepository.Object, _paymentRepository.Object,
            _particularRepository.Object, _expenseRepository.Object);

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
        payment.ApplyDiscount(350);
        payment.ProcessRefund(Guid.NewGuid());

        var handover = AmountHandover.Create(appId, session.Id, Guid.NewGuid(), Guid.NewGuid(), "Doctor", 1400, null);

        _repository.Setup(r => r.GetByIdAsync(handover.Id, It.IsAny<CancellationToken>())).ReturnsAsync(handover);
        _sessionRepository.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _paymentRepository.Setup(r => r.GetBySessionIdAsync(session.Id, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([payment]);

        var handler = CreateHandler();
        var action = () => handler.Handle(new AcceptAmountHandoverCommand(handover.Id, appId), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*counter session balance*");
        handover.Status.Should().Be(HandoverStatus.Pending);
    }
}
