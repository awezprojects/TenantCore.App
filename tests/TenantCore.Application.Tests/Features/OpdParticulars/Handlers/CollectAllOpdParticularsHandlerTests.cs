using FluentAssertions;
using Moq;
using TenantCore.Application.Features.OpdParticulars.Commands;
using TenantCore.Application.Features.OpdParticulars.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.OpdParticulars.Handlers;

public class CollectAllOpdParticularsHandlerTests
{
    private readonly Mock<IOpdParticularRepository> _opdParticularRepo = new();
    private readonly Mock<IOpdPaymentRepository> _paymentRepo = new();

    private CollectAllOpdParticularsHandler CreateHandler() =>
        new(_opdParticularRepo.Object, _paymentRepo.Object);

    [Fact]
    public async Task Handle_CollectsAllPendingItems_AndCreditsFullAmountToPayment()
    {
        // DEF-E2E-03 / TC-E2E-11: collect-all must bring OpdPayment.CollectedAmount up to the
        // visit's final amount, not leave it stuck at the visit-fee-only amount.
        var appId = Guid.NewGuid();
        var opdId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        var item1 = OpdParticular.Create(appId, opdId, Guid.NewGuid(), "USG", 500);
        var item2 = OpdParticular.Create(appId, opdId, Guid.NewGuid(), "Dressing", 150);

        var payment = OpdPayment.Create(appId, opdId, 200);
        payment.UpdateParticularsTotal(650);
        payment.AcceptVisitFee(userId, sessionId);

        _opdParticularRepo.Setup(r => r.GetByOpdRegistrationIdAsync(opdId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpdParticular> { item1, item2 });
        _paymentRepo.Setup(r => r.GetByOpdRegistrationIdAsync(opdId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        _opdParticularRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CollectAllOpdParticularsCommand(opdId, appId, userId, sessionId);
        var handler = CreateHandler();
        await handler.Handle(command, CancellationToken.None);

        item1.PaymentStatus.Should().Be(PaymentStatus.Received);
        item2.PaymentStatus.Should().Be(PaymentStatus.Received);
        payment.CollectedAmount.Should().Be(850); // 200 visit fee + 650 items
        payment.FinalAmount.Should().Be(850);
        _paymentRepo.Verify(r => r.Update(payment), Times.Once);
    }

    [Fact]
    public async Task Handle_NoPendingItems_DoesNotUpdatePayment()
    {
        var appId = Guid.NewGuid();
        var opdId = Guid.NewGuid();
        var item = OpdParticular.Create(appId, opdId, Guid.NewGuid(), "USG", 500);
        item.Collect(Guid.NewGuid(), Guid.NewGuid());
        var payment = OpdPayment.Create(appId, opdId, 200);

        _opdParticularRepo.Setup(r => r.GetByOpdRegistrationIdAsync(opdId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpdParticular> { item });
        _paymentRepo.Setup(r => r.GetByOpdRegistrationIdAsync(opdId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var command = new CollectAllOpdParticularsCommand(opdId, appId, Guid.NewGuid(), Guid.NewGuid());
        var handler = CreateHandler();
        await handler.Handle(command, CancellationToken.None);

        _paymentRepo.Verify(r => r.Update(It.IsAny<OpdPayment>()), Times.Never);
        _opdParticularRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDiscountApplied_Throws()
    {
        var appId = Guid.NewGuid();
        var opdId = Guid.NewGuid();
        var payment = OpdPayment.Create(appId, opdId, 200);
        payment.ApplyDiscount(50);

        _paymentRepo.Setup(r => r.GetByOpdRegistrationIdAsync(opdId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var command = new CollectAllOpdParticularsCommand(opdId, appId, Guid.NewGuid(), Guid.NewGuid());
        var handler = CreateHandler();
        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }
}
