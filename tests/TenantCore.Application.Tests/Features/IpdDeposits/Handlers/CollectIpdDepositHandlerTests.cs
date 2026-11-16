using FluentAssertions;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdDeposits.Commands;
using TenantCore.Application.Features.IpdDeposits.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.IpdDeposits.Handlers;

public class CollectIpdDepositHandlerTests
{
    private readonly Mock<IIpdRegistrationRepository> _ipdRepository = new();
    private readonly Mock<IIpdDepositRepository> _depositRepository = new();
    private readonly Mock<IIpdPaymentRepository> _paymentRepository = new();
    private readonly Mock<ICounterSessionRepository> _counterSessionRepository = new();
    private readonly Mock<IApplicationAccessValidator> _accessValidator = new();
    private readonly Guid _appId = Guid.NewGuid();

    public CollectIpdDepositHandlerTests()
    {
        _accessValidator.Setup(v => v.CanAccess(It.IsAny<Guid>())).Returns(true);
        _depositRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private CollectIpdDepositHandler CreateHandler() => new(
        _ipdRepository.Object, _depositRepository.Object, _paymentRepository.Object,
        _counterSessionRepository.Object, _accessValidator.Object);

    private IpdRegistration BuildAdmittedRegistration() =>
        IpdRegistration.Create(_appId, Guid.NewGuid(), Guid.NewGuid(), "Dr. Smith", "IPD-1", null, null, null, null, 0, null);

    [Fact]
    public async Task Handle_MultipleDeposits_EachAddsToPaymentTotalDeposited()
    {
        var registration = BuildAdmittedRegistration();
        var payment = IpdPayment.Create(_appId, registration.Id, 2000m);
        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _paymentRepository.Setup(r => r.GetByRegistrationAsync(registration.Id, _appId, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _counterSessionRepository.Setup(r => r.GetActiveSessionAsync(_appId, It.IsAny<CancellationToken>())).ReturnsAsync((CounterSession?)null);

        await CreateHandler().Handle(new CollectIpdDepositCommand(registration.Id, _appId, 5000m, Guid.NewGuid(), "First deposit"), CancellationToken.None);
        await CreateHandler().Handle(new CollectIpdDepositCommand(registration.Id, _appId, 3000m, Guid.NewGuid(), "Top-up"), CancellationToken.None);

        payment.TotalDeposited.Should().Be(8000m);
    }

    [Fact]
    public async Task Handle_NotAdmitted_ThrowsInvalidOperationException()
    {
        var registration = BuildAdmittedRegistration();
        registration.Discharge(null);
        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);

        var act = () => CreateHandler().Handle(new CollectIpdDepositCommand(registration.Id, _appId, 1000m, Guid.NewGuid(), null), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_RegistrationNotFound_ThrowsNotFoundException()
    {
        _ipdRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((IpdRegistration?)null);

        var act = () => CreateHandler().Handle(new CollectIpdDepositCommand(Guid.NewGuid(), _appId, 1000m, Guid.NewGuid(), null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_BillingDisabled_StillRecordsDepositWithoutPaymentRow()
    {
        var registration = BuildAdmittedRegistration();
        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _paymentRepository.Setup(r => r.GetByRegistrationAsync(registration.Id, _appId, It.IsAny<CancellationToken>())).ReturnsAsync((IpdPayment?)null);
        _counterSessionRepository.Setup(r => r.GetActiveSessionAsync(_appId, It.IsAny<CancellationToken>())).ReturnsAsync((CounterSession?)null);

        var result = await CreateHandler().Handle(new CollectIpdDepositCommand(registration.Id, _appId, 1000m, Guid.NewGuid(), null), CancellationToken.None);

        result.Amount.Should().Be(1000m);
        _paymentRepository.Verify(r => r.Update(It.IsAny<IpdPayment>()), Times.Never);
    }
}
