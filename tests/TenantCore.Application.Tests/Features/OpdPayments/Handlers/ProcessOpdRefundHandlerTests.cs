using FluentAssertions;
using Moq;
using TenantCore.Application.Features.OpdPayments.Commands;
using TenantCore.Application.Features.OpdPayments.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.OpdPayments.Handlers;

public class ProcessOpdRefundHandlerTests
{
    private readonly Mock<IOpdPaymentRepository> _repository = new();

    [Fact]
    public async Task Handle_PendingRefund_ReducesCollectedAmountAndMarksRefunded()
    {
        var appId = Guid.NewGuid();
        var opdId = Guid.NewGuid();
        var refundedBy = Guid.NewGuid();
        var payment = OpdPayment.Create(appId, opdId, 500);
        payment.AcceptVisitFee(Guid.NewGuid(), null);
        payment.ApplyDiscount(200); // FinalAmount(300) < CollectedAmount(500) -> RefundDue 200

        _repository.Setup(r => r.GetByOpdRegistrationIdAsync(opdId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new ProcessOpdRefundHandler(_repository.Object);
        var command = new ProcessOpdRefundCommand(new ProcessOpdRefundRequest { OpdRegistrationId = opdId }, refundedBy, appId);
        var result = await handler.Handle(command, CancellationToken.None);

        result.RefundStatus.Should().Be(RefundStatus.Refunded);
        result.RefundDue.Should().Be(0);
        payment.CollectedAmount.Should().Be(300);
        payment.RefundedByUserId.Should().Be(refundedBy);
        _repository.Verify(r => r.Update(payment), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PaymentNotFound_ThrowsNotFoundException()
    {
        var appId = Guid.NewGuid();
        var opdId = Guid.NewGuid();
        _repository.Setup(r => r.GetByOpdRegistrationIdAsync(opdId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpdPayment?)null);

        var handler = new ProcessOpdRefundHandler(_repository.Object);
        var action = () => handler.Handle(
            new ProcessOpdRefundCommand(new ProcessOpdRefundRequest { OpdRegistrationId = opdId }, Guid.NewGuid(), appId),
            CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_NoRefundPending_ThrowsInvalidOperationException()
    {
        var appId = Guid.NewGuid();
        var opdId = Guid.NewGuid();
        var payment = OpdPayment.Create(appId, opdId, 500);
        payment.AcceptVisitFee(Guid.NewGuid(), null);

        _repository.Setup(r => r.GetByOpdRegistrationIdAsync(opdId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var handler = new ProcessOpdRefundHandler(_repository.Object);
        var action = () => handler.Handle(
            new ProcessOpdRefundCommand(new ProcessOpdRefundRequest { OpdRegistrationId = opdId }, Guid.NewGuid(), appId),
            CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }
}
