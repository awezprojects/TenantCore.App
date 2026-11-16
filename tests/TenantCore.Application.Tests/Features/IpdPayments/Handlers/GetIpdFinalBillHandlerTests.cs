using FluentAssertions;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdPayments.Handlers;
using TenantCore.Application.Features.IpdPayments.Queries;
using TenantCore.Application.Features.IpdPayments.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.IpdPayments.Handlers;

public class GetIpdFinalBillHandlerTests
{
    private readonly Mock<IIpdRegistrationRepository> _ipdRepository = new();
    private readonly Mock<IIpdPaymentRepository> _paymentRepository = new();
    private readonly Mock<IIpdBillCalculator> _billCalculator = new();
    private readonly Mock<IApplicationAccessValidator> _accessValidator = new();
    private readonly Guid _appId = Guid.NewGuid();

    public GetIpdFinalBillHandlerTests() => _accessValidator.Setup(v => v.CanAccess(_appId)).Returns(true);

    private GetIpdFinalBillHandler CreateHandler() => new(
        _ipdRepository.Object, _paymentRepository.Object, _billCalculator.Object, _accessValidator.Object);

    [Fact]
    public async Task Handle_NotYetFinalized_ThrowsInvalidOperationException()
    {
        var ipdRegistrationId = Guid.NewGuid();
        var payment = IpdPayment.Create(_appId, ipdRegistrationId, 2000m); // still admitted, never finalized
        _paymentRepository.Setup(r => r.GetByRegistrationAsync(ipdRegistrationId, _appId, It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        var act = () => CreateHandler().Handle(new GetIpdFinalBillQuery(ipdRegistrationId, _appId), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_Finalized_ReturnsLockedBill()
    {
        var ipdRegistrationId = Guid.NewGuid();
        var payment = IpdPayment.Create(_appId, ipdRegistrationId, 2000m);
        var registration = IpdRegistration.Create(_appId, Guid.NewGuid(), Guid.NewGuid(), "Dr. Smith", "IPD-1", null, null, null, null, 2000m, null);
        payment.FinalizeAtDischarge(500m, DateTime.UtcNow);
        _paymentRepository.Setup(r => r.GetByRegistrationAsync(ipdRegistrationId, _appId, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _ipdRepository.Setup(r => r.GetByIdAsync(ipdRegistrationId, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _billCalculator.Setup(c => c.CalculateRoomChargesAsync(registration, payment.FinalizedAt!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IpdBillCalculationResult(500m, [], 0));

        var result = await CreateHandler().Handle(new GetIpdFinalBillQuery(ipdRegistrationId, _appId), CancellationToken.None);

        result.FinalAmount.Should().Be(2500m);
        result.WasCancelled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_CancelledAdmission_SetsWasCancelledTrue()
    {
        // DEF-IPD-22 (14-Nov QA): without this flag, a cancelled stay's bill showed
        // "FINAL BILL — Locked at discharge" next to a non-zero InitialFee while Total was ₹0.00.
        var ipdRegistrationId = Guid.NewGuid();
        var payment = IpdPayment.Create(_appId, ipdRegistrationId, 100m);
        var registration = IpdRegistration.Create(_appId, Guid.NewGuid(), Guid.NewGuid(), "Dr. Smith", "IPD-1", null, null, null, null, 100m, null);
        registration.Cancel("Wrong patient admitted");
        payment.FinalizeAtCancellation(DateTime.UtcNow);
        _paymentRepository.Setup(r => r.GetByRegistrationAsync(ipdRegistrationId, _appId, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _ipdRepository.Setup(r => r.GetByIdAsync(ipdRegistrationId, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _billCalculator.Setup(c => c.CalculateRoomChargesAsync(registration, payment.FinalizedAt!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IpdBillCalculationResult(0m, [], 0));

        var result = await CreateHandler().Handle(new GetIpdFinalBillQuery(ipdRegistrationId, _appId), CancellationToken.None);

        result.WasCancelled.Should().BeTrue();
        result.FinalAmount.Should().Be(0m);
    }
}
