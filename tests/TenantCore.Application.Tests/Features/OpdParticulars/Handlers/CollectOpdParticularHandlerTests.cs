using FluentAssertions;
using Moq;
using TenantCore.Application.Features.OpdParticulars.Commands;
using TenantCore.Application.Features.OpdParticulars.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.OpdParticulars.Handlers;

public class CollectOpdParticularHandlerTests
{
    private readonly Mock<IOpdParticularRepository> _opdParticularRepo = new();
    private readonly Mock<IOpdPaymentRepository> _paymentRepo = new();

    private CollectOpdParticularHandler CreateHandler() =>
        new(_opdParticularRepo.Object, _paymentRepo.Object);

    [Fact]
    public async Task Handle_WhenPaymentExists_FoldsItemAmountIntoCollectedAmount()
    {
        // DEF-E2E-03: collecting an item must update OpdPayment.CollectedAmount, not just the item itself.
        var appId = Guid.NewGuid();
        var opdId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        var particular = OpdParticular.Create(appId, opdId, Guid.NewGuid(), "USG", 500);
        var payment = OpdPayment.Create(appId, opdId, 200);
        payment.UpdateParticularsTotal(500);
        payment.AcceptVisitFee(userId, sessionId);

        _opdParticularRepo.Setup(r => r.GetByIdAsync(particular.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(particular);
        _paymentRepo.Setup(r => r.GetByOpdRegistrationIdAsync(opdId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        _opdParticularRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CollectOpdParticularCommand(particular.Id, appId, userId, sessionId);
        var handler = CreateHandler();
        await handler.Handle(command, CancellationToken.None);

        particular.PaymentStatus.Should().Be(PaymentStatus.Received);
        payment.CollectedAmount.Should().Be(700);
        _paymentRepo.Verify(r => r.Update(payment), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDiscountApplied_Throws()
    {
        var appId = Guid.NewGuid();
        var opdId = Guid.NewGuid();
        var particular = OpdParticular.Create(appId, opdId, Guid.NewGuid(), "USG", 500);
        var payment = OpdPayment.Create(appId, opdId, 200);
        payment.ApplyDiscount(50, Guid.NewGuid());

        _opdParticularRepo.Setup(r => r.GetByIdAsync(particular.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(particular);
        _paymentRepo.Setup(r => r.GetByOpdRegistrationIdAsync(opdId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var command = new CollectOpdParticularCommand(particular.Id, appId, Guid.NewGuid(), Guid.NewGuid());
        var handler = CreateHandler();
        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_WhenItemNotFound_ThrowsNotFoundException()
    {
        _opdParticularRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpdParticular?)null);

        var command = new CollectOpdParticularCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var handler = CreateHandler();
        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }
}
