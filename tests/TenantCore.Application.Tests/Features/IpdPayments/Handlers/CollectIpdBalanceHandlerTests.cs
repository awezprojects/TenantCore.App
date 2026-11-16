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

public class CollectIpdBalanceHandlerTests
{
    private readonly Mock<IIpdRegistrationRepository> _ipdRepository = new();
    private readonly Mock<IIpdPaymentRepository> _paymentRepository = new();
    private readonly Mock<ICounterSessionRepository> _counterSessionRepository = new();
    private readonly Mock<IIpdBillCalculator> _billCalculator = new();
    private readonly Mock<IApplicationAccessValidator> _accessValidator = new();
    private readonly Guid _appId = Guid.NewGuid();

    public CollectIpdBalanceHandlerTests()
    {
        _accessValidator.Setup(v => v.CanAccess(It.IsAny<Guid>())).Returns(true);
        _paymentRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _billCalculator.Setup(c => c.CalculateRoomChargesAsync(It.IsAny<IpdRegistration>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IpdBillCalculationResult(0, [], 0));
    }

    private CollectIpdBalanceHandler CreateHandler() => new(
        _ipdRepository.Object, _paymentRepository.Object, _counterSessionRepository.Object, _billCalculator.Object, _accessValidator.Object);

    [Fact]
    public async Task Handle_FinalizedWithBalanceDue_CollectsAndMarksReceived()
    {
        var ipdRegistrationId = Guid.NewGuid();
        var payment = IpdPayment.Create(_appId, ipdRegistrationId, 2000m);
        payment.FinalizeAtDischarge(0, DateTime.UtcNow); // BalanceDue = 2000
        var registration = IpdRegistration.Create(_appId, Guid.NewGuid(), Guid.NewGuid(), "Dr. Smith", "IPD-1", null, null, null, null, 2000m, null);
        _paymentRepository.Setup(r => r.GetByRegistrationAsync(ipdRegistrationId, _appId, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _ipdRepository.Setup(r => r.GetByIdAsync(ipdRegistrationId, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _counterSessionRepository.Setup(r => r.GetActiveSessionAsync(_appId, It.IsAny<CancellationToken>())).ReturnsAsync((CounterSession?)null);

        var result = await CreateHandler().Handle(new CollectIpdBalanceCommand(ipdRegistrationId, _appId, Guid.NewGuid()), CancellationToken.None);

        result.PaymentStatus.Should().Be(PaymentStatus.Received);
        payment.SettledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_NotFinalized_ThrowsInvalidOperationException()
    {
        var ipdRegistrationId = Guid.NewGuid();
        var payment = IpdPayment.Create(_appId, ipdRegistrationId, 2000m); // never finalized
        _paymentRepository.Setup(r => r.GetByRegistrationAsync(ipdRegistrationId, _appId, It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        var act = () => CreateHandler().Handle(new CollectIpdBalanceCommand(ipdRegistrationId, _appId, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_PaymentNotFound_ThrowsNotFoundException()
    {
        _paymentRepository.Setup(r => r.GetByRegistrationAsync(It.IsAny<Guid>(), _appId, It.IsAny<CancellationToken>())).ReturnsAsync((IpdPayment?)null);

        var act = () => CreateHandler().Handle(new CollectIpdBalanceCommand(Guid.NewGuid(), _appId, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
