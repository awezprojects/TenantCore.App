using FluentAssertions;
using Moq;
using TenantCore.Application.Features.OpdPayments.Commands;
using TenantCore.Application.Features.OpdPayments.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Tests.Features.OpdPayments.Handlers;

public class ApplyOpdDiscountHandlerTests
{
    private readonly Mock<IOpdPaymentRepository> _repository = new();

    private ApplyOpdDiscountHandler CreateHandler() => new(_repository.Object);

    [Fact]
    public async Task Handle_ValidDiscount_RecordsWhoAppliedIt()
    {
        var appId = Guid.NewGuid();
        var opdId = Guid.NewGuid();
        var discountedBy = Guid.NewGuid();
        var payment = OpdPayment.Create(appId, opdId, 500);

        _repository.Setup(r => r.GetByOpdRegistrationIdAsync(opdId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var command = new ApplyOpdDiscountCommand(
            new ApplyOpdDiscountRequest { OpdRegistrationId = opdId, Discount = 100 }, discountedBy, appId);
        var result = await handler.Handle(command, CancellationToken.None);

        result.Discount.Should().Be(100);
        result.DiscountedByUserId.Should().Be(discountedBy);
        payment.DiscountedByUserId.Should().Be(discountedBy);
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

        var handler = CreateHandler();
        var action = () => handler.Handle(
            new ApplyOpdDiscountCommand(
                new ApplyOpdDiscountRequest { OpdRegistrationId = opdId, Discount = 50 }, Guid.NewGuid(), appId),
            CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DiscountExceedsTotal_ThrowsInvalidOperationException()
    {
        var appId = Guid.NewGuid();
        var opdId = Guid.NewGuid();
        var payment = OpdPayment.Create(appId, opdId, 500);

        _repository.Setup(r => r.GetByOpdRegistrationIdAsync(opdId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var handler = CreateHandler();
        var action = () => handler.Handle(
            new ApplyOpdDiscountCommand(
                new ApplyOpdDiscountRequest { OpdRegistrationId = opdId, Discount = 600 }, Guid.NewGuid(), appId),
            CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }
}
