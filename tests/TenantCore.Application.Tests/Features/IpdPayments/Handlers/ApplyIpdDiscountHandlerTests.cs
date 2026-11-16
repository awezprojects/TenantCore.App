using FluentAssertions;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdPayments.Commands;
using TenantCore.Application.Features.IpdPayments.Handlers;
using TenantCore.Application.Features.IpdPayments.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.IpdPayments.Handlers;

public class ApplyIpdDiscountHandlerTests
{
    private readonly Mock<IIpdRegistrationRepository> _ipdRepository = new();
    private readonly Mock<IIpdPaymentRepository> _paymentRepository = new();
    private readonly Mock<IIpdBillCalculator> _billCalculator = new();
    private readonly Mock<IApplicationAccessValidator> _accessValidator = new();
    private readonly Guid _appId = Guid.NewGuid();

    public ApplyIpdDiscountHandlerTests()
    {
        _accessValidator.Setup(v => v.CanAccess(It.IsAny<Guid>())).Returns(true);
        _paymentRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private ApplyIpdDiscountHandler CreateHandler() => new(
        _ipdRepository.Object, _paymentRepository.Object, _billCalculator.Object, _accessValidator.Object);

    private IpdRegistration BuildAdmittedRegistration() =>
        IpdRegistration.Create(_appId, Guid.NewGuid(), Guid.NewGuid(), "Dr. Smith", "IPD-1", null, null, null, null, 0, null);

    [Fact]
    public async Task Handle_ValidDiscount_UpdatesPaymentAndReturnsRecalculatedBalance()
    {
        var registration = BuildAdmittedRegistration();
        var payment = IpdPayment.Create(_appId, registration.Id, 2000m); // InitialFee only, no particulars/room charges yet
        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _paymentRepository.Setup(r => r.GetByRegistrationAsync(registration.Id, _appId, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _billCalculator.Setup(c => c.CalculateRoomChargesAsync(registration, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IpdBillCalculationResult(0, [], 0));

        var result = await CreateHandler().Handle(
            new ApplyIpdDiscountCommand(registration.Id, _appId, 500m, Guid.NewGuid()), CancellationToken.None);

        payment.Discount.Should().Be(500m);
        result.FinalAmount.Should().Be(1500m); // 2000 initial - 500 discount
        result.IsProvisional.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DiscountExceedsTotal_ThrowsInvalidOperationException()
    {
        var registration = BuildAdmittedRegistration();
        var payment = IpdPayment.Create(_appId, registration.Id, 2000m);
        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _paymentRepository.Setup(r => r.GetByRegistrationAsync(registration.Id, _appId, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _billCalculator.Setup(c => c.CalculateRoomChargesAsync(registration, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IpdBillCalculationResult(0, [], 0));

        var act = () => CreateHandler().Handle(
            new ApplyIpdDiscountCommand(registration.Id, _appId, 5000m, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_PaymentNotFound_ThrowsNotFoundException()
    {
        var registration = BuildAdmittedRegistration();
        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _paymentRepository.Setup(r => r.GetByRegistrationAsync(registration.Id, _appId, It.IsAny<CancellationToken>())).ReturnsAsync((IpdPayment?)null);

        var act = () => CreateHandler().Handle(
            new ApplyIpdDiscountCommand(registration.Id, _appId, 100m, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
