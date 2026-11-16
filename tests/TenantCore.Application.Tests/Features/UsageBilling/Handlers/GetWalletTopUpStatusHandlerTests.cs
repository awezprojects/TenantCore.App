using FluentAssertions;
using Moq;
using TenantCore.Application.Features.UsageBilling.Commands;
using TenantCore.Application.Features.UsageBilling.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.UsageBilling.Handlers;

public class GetWalletTopUpStatusHandlerTests
{
    private readonly Mock<ISubscriptionPaymentRepository> _paymentRepository = new();
    private readonly Guid _appId = Guid.NewGuid();

    private GetWalletTopUpStatusHandler CreateHandler() => new(_paymentRepository.Object);

    [Fact]
    public async Task Handle_PaidWalletTopUp_ReturnsDtoWithNoLinkUrl()
    {
        // Unlike GetActiveWalletTopUpQuery, a Paid payment must still be returned here — this is
        // exactly what the client polls to detect the paid transition and stop showing the link.
        var payment = SubscriptionPayment.CreateForWalletTopUp(
            _appId, 1000m, "INR", "Clinic Admin", "admin@clinic.test", null, "Test Clinic");
        payment.SetLink("link_123", "https://rzp.io/i/abc", null);
        payment.MarkPaid("pay_123", "upi");

        _paymentRepository.Setup(r => r.GetByIdForClinicAsync(payment.Id, _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var result = await CreateHandler().Handle(new GetWalletTopUpStatusQuery(_appId, payment.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Status.Should().Be(SubscriptionPaymentStatus.Paid);
        result.PaymentLinkUrl.Should().BeNull();
    }

    [Fact]
    public async Task Handle_PaymentNotFound_ReturnsNull()
    {
        _paymentRepository.Setup(r => r.GetByIdForClinicAsync(It.IsAny<Guid>(), _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPayment?)null);

        var result = await CreateHandler().Handle(new GetWalletTopUpStatusQuery(_appId, Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_PaymentIsADifferentPurpose_ReturnsNull()
    {
        var registrationFee = SubscriptionPayment.CreateForRegistrationFee(
            _appId, 500m, "INR", "Clinic Admin", "admin@clinic.test", null, "Test Clinic");

        _paymentRepository.Setup(r => r.GetByIdForClinicAsync(registrationFee.Id, _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(registrationFee);

        var result = await CreateHandler().Handle(new GetWalletTopUpStatusQuery(_appId, registrationFee.Id), CancellationToken.None);

        result.Should().BeNull();
    }
}
