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

public class AcceptOpdPaymentHandlerTests
{
    private readonly Mock<IOpdPaymentRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenPending_AcceptsPayment()
    {
        var appId = Guid.NewGuid();
        var opdId = Guid.NewGuid();
        var payment = OpdPayment.Create(appId, opdId, 500);
        var receivedBy = Guid.NewGuid();

        _repository.Setup(r => r.GetByOpdRegistrationIdAsync(opdId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        _repository.Setup(r => r.Update(payment));
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new AcceptOpdPaymentCommand(
            new AcceptOpdPaymentRequest { OpdRegistrationId = opdId }, receivedBy, appId);
        var handler = new AcceptOpdPaymentHandler(_repository.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        result.PaymentStatus.Should().Be(PaymentStatus.Received);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ThrowsNotFoundException()
    {
        var appId = Guid.NewGuid();
        var opdId = Guid.NewGuid();
        _repository.Setup(r => r.GetByOpdRegistrationIdAsync(opdId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpdPayment?)null);

        var handler = new AcceptOpdPaymentHandler(_repository.Object);
        var action = () => handler.Handle(
            new AcceptOpdPaymentCommand(
                new AcceptOpdPaymentRequest { OpdRegistrationId = opdId },
                Guid.NewGuid(), appId),
            CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenAlreadyReceived_ThrowsInvalidOperationException()
    {
        var appId = Guid.NewGuid();
        var opdId = Guid.NewGuid();
        var payment = OpdPayment.Create(appId, opdId, 500);
        payment.Accept(Guid.NewGuid(), null);

        _repository.Setup(r => r.GetByOpdRegistrationIdAsync(opdId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var handler = new AcceptOpdPaymentHandler(_repository.Object);
        var action = () => handler.Handle(
            new AcceptOpdPaymentCommand(
                new AcceptOpdPaymentRequest { OpdRegistrationId = opdId },
                Guid.NewGuid(), appId),
            CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }
}
