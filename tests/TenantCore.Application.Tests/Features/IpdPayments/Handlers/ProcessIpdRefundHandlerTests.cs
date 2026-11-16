using FluentAssertions;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdPayments.Commands;
using TenantCore.Application.Features.IpdPayments.Handlers;
using TenantCore.Application.Features.IpdPayments.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.IpdPayments.Handlers;

public class ProcessIpdRefundHandlerTests
{
    private readonly Mock<IIpdRegistrationRepository> _ipdRepository = new();
    private readonly Mock<IIpdPaymentRepository> _paymentRepository = new();
    private readonly Mock<IIpdBillCalculator> _billCalculator = new();
    private readonly Mock<IApplicationAccessValidator> _accessValidator = new();
    private readonly Guid _appId = Guid.NewGuid();

    public ProcessIpdRefundHandlerTests()
    {
        _accessValidator.Setup(v => v.CanAccess(It.IsAny<Guid>())).Returns(true);
        _paymentRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _billCalculator.Setup(c => c.CalculateRoomChargesAsync(It.IsAny<IpdRegistration>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IpdBillCalculationResult(0, [], 0));
    }

    private ProcessIpdRefundHandler CreateHandler() => new(
        _ipdRepository.Object, _paymentRepository.Object, _billCalculator.Object, _accessValidator.Object);

    [Fact]
    public async Task Handle_RefundPending_MarksRefunded()
    {
        var ipdRegistrationId = Guid.NewGuid();
        var payment = IpdPayment.Create(_appId, ipdRegistrationId, 2000m);
        payment.IncreaseDeposited(5000m);
        payment.FinalizeAtDischarge(0, DateTime.UtcNow); // RefundDue = 3000, PendingRefund
        var registration = IpdRegistration.Create(_appId, Guid.NewGuid(), Guid.NewGuid(), "Dr. Smith", "IPD-1", null, null, null, null, 2000m, null);
        _paymentRepository.Setup(r => r.GetByRegistrationAsync(ipdRegistrationId, _appId, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _ipdRepository.Setup(r => r.GetByIdAsync(ipdRegistrationId, It.IsAny<CancellationToken>())).ReturnsAsync(registration);

        var result = await CreateHandler().Handle(new ProcessIpdRefundCommand(ipdRegistrationId, _appId, Guid.NewGuid()), CancellationToken.None);

        result.RefundStatus.Should().Be(RefundStatus.Refunded);
        payment.RefundedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_NoRefundPending_ThrowsInvalidOperationException()
    {
        var ipdRegistrationId = Guid.NewGuid();
        var payment = IpdPayment.Create(_appId, ipdRegistrationId, 2000m); // RefundStatus.None
        _paymentRepository.Setup(r => r.GetByRegistrationAsync(ipdRegistrationId, _appId, It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        var act = () => CreateHandler().Handle(new ProcessIpdRefundCommand(ipdRegistrationId, _appId, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
