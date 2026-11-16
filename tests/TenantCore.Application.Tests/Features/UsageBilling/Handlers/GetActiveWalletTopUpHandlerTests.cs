using FluentAssertions;
using Moq;
using TenantCore.Application.Features.UsageBilling.Commands;
using TenantCore.Application.Features.UsageBilling.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.UsageBilling.Handlers;

public class GetActiveWalletTopUpHandlerTests
{
    private readonly Mock<ISubscriptionPaymentRepository> _paymentRepository = new();
    private readonly Guid _appId = Guid.NewGuid();

    private GetActiveWalletTopUpHandler CreateHandler() => new(_paymentRepository.Object);

    [Fact]
    public async Task Handle_OpenWalletTopUpExists_ReturnsDto()
    {
        var payment = SubscriptionPayment.CreateForWalletTopUp(
            _appId, 1000m, "INR", "Clinic Admin", "admin@clinic.test", null, "Test Clinic");

        _paymentRepository.Setup(r => r.GetOpenLinkForClinicAsync(_appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var result = await CreateHandler().Handle(new GetActiveWalletTopUpQuery(_appId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.PaymentId.Should().Be(payment.Id);
        result.Amount.Should().Be(1000m);
    }

    [Fact]
    public async Task Handle_NoOpenLink_ReturnsNull()
    {
        _paymentRepository.Setup(r => r.GetOpenLinkForClinicAsync(_appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPayment?)null);

        var result = await CreateHandler().Handle(new GetActiveWalletTopUpQuery(_appId), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_OpenLinkIsADifferentPurpose_ReturnsNull()
    {
        // Regression guard: GetOpenLinkForClinicAsync is shared across every "one open clinic link"
        // purpose — this must never hand back someone else's open link (e.g. a renewal) as if it
        // were a wallet top-up.
        var renewal = SubscriptionPayment.CreateForRenewal(
            _appId, Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly",
            999m, "INR", "Clinic Admin", "admin@clinic.test", null, Guid.NewGuid());

        _paymentRepository.Setup(r => r.GetOpenLinkForClinicAsync(_appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(renewal);

        var result = await CreateHandler().Handle(new GetActiveWalletTopUpQuery(_appId), CancellationToken.None);

        result.Should().BeNull();
    }
}
