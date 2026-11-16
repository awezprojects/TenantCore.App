using FluentAssertions;
using Moq;
using TenantCore.Application.Features.IpdPayments.Commands;
using TenantCore.Application.Features.IpdPayments.Handlers;
using TenantCore.Application.Features.IpdPayments.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.IpdPayments.Handlers;

public class FinalizeIpdBillHandlerTests
{
    private readonly Mock<IIpdRegistrationRepository> _ipdRepository = new();
    private readonly Mock<IIpdPaymentRepository> _paymentRepository = new();
    private readonly Mock<IIpdBillCalculator> _billCalculator = new();
    private readonly Guid _appId = Guid.NewGuid();

    private FinalizeIpdBillHandler CreateHandler() => new(_ipdRepository.Object, _paymentRepository.Object, _billCalculator.Object);

    private IpdRegistration BuildDischargedRegistration()
    {
        var registration = IpdRegistration.Create(_appId, Guid.NewGuid(), Guid.NewGuid(), "Dr. Smith", "IPD-1", null, null, null, null, 2000m, null);
        registration.Discharge(null);
        return registration;
    }

    [Fact]
    public async Task Handle_BillingDisabled_ReturnsNullWithoutError()
    {
        _paymentRepository.Setup(r => r.GetByRegistrationAsync(It.IsAny<Guid>(), _appId, It.IsAny<CancellationToken>())).ReturnsAsync((IpdPayment?)null);

        var result = await CreateHandler().Handle(new FinalizeIpdBillCommand(Guid.NewGuid(), _appId, DateTime.UtcNow), CancellationToken.None);

        result.Should().BeNull();
        _ipdRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BalanceOwed_LocksBillAsPendingWithNoRefund()
    {
        var registration = BuildDischargedRegistration();
        var payment = IpdPayment.Create(_appId, registration.Id, 2000m);
        payment.IncreaseDeposited(500m); // less than the final amount — balance remains due
        var dischargeDate = DateTime.UtcNow;
        _paymentRepository.Setup(r => r.GetByRegistrationAsync(registration.Id, _appId, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _billCalculator.Setup(c => c.CalculateRoomChargesAsync(registration, dischargeDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IpdBillCalculationResult(1000m, [], 0));

        var result = await CreateHandler().Handle(new FinalizeIpdBillCommand(registration.Id, _appId, dischargeDate), CancellationToken.None);

        result!.FinalAmount.Should().Be(3000m); // 2000 initial + 1000 room
        result.BalanceDue.Should().Be(2500m); // 3000 - 500 deposited
        result.PaymentStatus.Should().Be(PaymentStatus.Pending);
        result.RefundDue.Should().Be(0);
        _paymentRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never); // sub-command: outer handler saves
    }

    [Fact]
    public async Task Handle_DepositExceedsFinalBill_QueuesRefundAndMarksReceived()
    {
        var registration = BuildDischargedRegistration();
        var payment = IpdPayment.Create(_appId, registration.Id, 2000m);
        payment.IncreaseDeposited(5000m); // more than the final amount — a refund is owed
        var dischargeDate = DateTime.UtcNow;
        _paymentRepository.Setup(r => r.GetByRegistrationAsync(registration.Id, _appId, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _billCalculator.Setup(c => c.CalculateRoomChargesAsync(registration, dischargeDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IpdBillCalculationResult(0, [], 0));

        var result = await CreateHandler().Handle(new FinalizeIpdBillCommand(registration.Id, _appId, dischargeDate), CancellationToken.None);

        result!.BalanceDue.Should().Be(-3000m); // 2000 final - 5000 deposited
        result.RefundDue.Should().Be(3000m);
        result.RefundStatus.Should().Be(RefundStatus.PendingRefund);
        result.PaymentStatus.Should().Be(PaymentStatus.Received); // nothing further to collect
    }
}
