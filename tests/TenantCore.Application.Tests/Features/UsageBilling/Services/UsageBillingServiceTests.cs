using FluentAssertions;
using Moq;
using TenantCore.Application.Features.UsageBilling.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.UsageBilling.Services;

/// <summary>
/// The single place the mode, the price and the chargeable amount are resolved. Two rules matter most
/// here: a clinic with no account row behaves exactly as it did before usage billing existed
/// (<see cref="BillingMode.Subscription"/>, nothing to pay), and a queued mode only counts once its
/// instant has passed — the same rule the repository projection applies.
/// </summary>
public class UsageBillingServiceTests
{
    private readonly Mock<IClinicAccountRepository> _accounts = new();
    private readonly Mock<IClinicUsageRateRepository> _rates = new();
    private readonly Guid _applicationId = Guid.NewGuid();

    private UsageBillingService CreateService() => new(_accounts.Object, _rates.Object);

    private ClinicAccount GivenAccount()
    {
        var account = ClinicAccount.CreateDefault(_applicationId);
        _accounts.Setup(r => r.GetByApplicationIdAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        return account;
    }

    [Fact]
    public async Task ResolveEffectiveMode_NoAccountRow_IsSubscription()
    {
        _accounts.Setup(r => r.GetByApplicationIdAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicAccount?)null);

        var mode = await CreateService().ResolveEffectiveModeAsync(_applicationId);

        mode.Should().Be(BillingMode.Subscription, "today's behaviour is the default");
        _accounts.Verify(r => r.AddAsync(It.IsAny<ClinicAccount>(), It.IsAny<CancellationToken>()), Times.Never,
            "a row is never created just to answer the question");
    }

    [Fact]
    public async Task ResolveEffectiveMode_QueuedModeNotYetDue_StaysOnTheCurrentMode()
    {
        var account = GivenAccount();
        account.SetBillingMode(BillingMode.PayAsYouGo, DateTime.UtcNow, applyNow: false,
            effectiveFrom: DateTime.UtcNow.AddDays(15));

        var mode = await CreateService().ResolveEffectiveModeAsync(_applicationId);

        mode.Should().Be(BillingMode.Subscription);
    }

    [Fact]
    public async Task ResolveEffectiveMode_QueuedModeAlreadyDue_ReportsTheQueuedMode()
    {
        var account = GivenAccount();
        account.SetBillingMode(BillingMode.PayAsYouGo, DateTime.UtcNow, applyNow: false,
            effectiveFrom: DateTime.UtcNow.AddSeconds(-5));

        var mode = await CreateService().ResolveEffectiveModeAsync(_applicationId);

        mode.Should().Be(BillingMode.PayAsYouGo);
    }

    [Fact]
    public async Task ResolveRate_NoActiveRow_IsZeroSoTheOperationIsFree()
    {
        _rates.Setup(r => r.GetActiveAsync(_applicationId, BillingItem.OpdRegistration, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicUsageRate?)null);

        var rate = await CreateService().ResolveRateAsync(_applicationId, BillingItem.OpdRegistration);

        rate.Should().Be(0m, "an unpriced operation never blocks a clinic");
    }

    [Fact]
    public async Task ResolveRate_ActiveRow_ReturnsTheClinicsOwnPrice()
    {
        _rates.Setup(r => r.GetActiveAsync(_applicationId, BillingItem.OpdRegistration, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ClinicUsageRate.Create(
                _applicationId, BillingItem.OpdRegistration, 15m, DateTime.UtcNow, "admin@example.test"));

        var rate = await CreateService().ResolveRateAsync(_applicationId, BillingItem.OpdRegistration);

        rate.Should().Be(15m);
    }

    [Fact]
    public async Task ResolveRates_MapsEveryActiveRowByItem()
    {
        _rates.Setup(r => r.GetActiveForClinicAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ClinicUsageRate>
            {
                ClinicUsageRate.Create(_applicationId, BillingItem.OpdRegistration, 15m, DateTime.UtcNow, "a@b.test"),
                ClinicUsageRate.Create(_applicationId, BillingItem.IpdAdmission, 40m, DateTime.UtcNow, "a@b.test")
            });

        var rates = await CreateService().ResolveRatesAsync(_applicationId);

        rates.Should().HaveCount(2);
        rates[BillingItem.OpdRegistration].Should().Be(15m);
        rates[BillingItem.IpdAdmission].Should().Be(40m);
    }

    [Theory]
    [InlineData(12.345, 3, 37.04)]
    [InlineData(15, 1, 15)]
    [InlineData(0.005, 1, 0.01)]
    public void ResolveChargeableAmount_RoundsAwayFromZeroToTwoPlaces(decimal rate, int quantity, decimal expected)
    {
        CreateService().ResolveChargeableAmount(rate, quantity).Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(-1, 5)]
    [InlineData(15, 0)]
    [InlineData(15, -3)]
    public void ResolveChargeableAmount_FreeRateOrNoQuantity_CostsNothing(decimal rate, int quantity)
    {
        CreateService().ResolveChargeableAmount(rate, quantity).Should().Be(0m);
    }
}
