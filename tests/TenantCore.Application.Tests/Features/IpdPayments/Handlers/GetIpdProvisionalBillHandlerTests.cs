using FluentAssertions;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdPayments.Handlers;
using TenantCore.Application.Features.IpdPayments.Queries;
using TenantCore.Application.Features.IpdPayments.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.IpdPayments.Handlers;

public class GetIpdProvisionalBillHandlerTests
{
    private readonly Mock<IIpdRegistrationRepository> _ipdRepository = new();
    private readonly Mock<IIpdPaymentRepository> _paymentRepository = new();
    private readonly Mock<IIpdBillCalculator> _billCalculator = new();
    private readonly Mock<IApplicationAccessValidator> _accessValidator = new();
    private readonly Guid _appId = Guid.NewGuid();

    private GetIpdProvisionalBillHandler CreateHandler() => new(
        _ipdRepository.Object, _paymentRepository.Object, _billCalculator.Object, _accessValidator.Object);

    [Fact]
    public async Task Handle_ActiveAdmission_ReturnsProvisionalFlagTrueAndNeverPersists()
    {
        _accessValidator.Setup(v => v.CanAccess(_appId)).Returns(true);
        var registration = IpdRegistration.Create(_appId, Guid.NewGuid(), Guid.NewGuid(), "Dr. Smith", "IPD-1", null, null, null, null, 2000m, null);
        var payment = IpdPayment.Create(_appId, registration.Id, 2000m);
        payment.IncreaseDeposited(1000m);
        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _paymentRepository.Setup(r => r.GetByRegistrationAsync(registration.Id, _appId, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _billCalculator.Setup(c => c.CalculateRoomChargesAsync(registration, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IpdBillCalculationResult(1500m, [], 0));

        var result = await CreateHandler().Handle(new GetIpdProvisionalBillQuery(registration.Id, _appId), CancellationToken.None);

        result.IsProvisional.Should().BeTrue();
        result.TotalAmount.Should().Be(3500m); // 2000 initial + 1500 room
        result.BalanceDue.Should().Be(2500m); // 3500 - 1000 deposited
        _paymentRepository.Verify(r => r.Update(It.IsAny<IpdPayment>()), Times.Never);
        _paymentRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PaymentNotFound_ThrowsNotFoundException()
    {
        _accessValidator.Setup(v => v.CanAccess(_appId)).Returns(true);
        var registration = IpdRegistration.Create(_appId, Guid.NewGuid(), Guid.NewGuid(), "Dr. Smith", "IPD-1", null, null, null, null, 2000m, null);
        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _paymentRepository.Setup(r => r.GetByRegistrationAsync(registration.Id, _appId, It.IsAny<CancellationToken>())).ReturnsAsync((IpdPayment?)null);

        var act = () => CreateHandler().Handle(new GetIpdProvisionalBillQuery(registration.Id, _appId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
