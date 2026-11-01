using FluentAssertions;
using Microsoft.Extensions.Options;
using TenantCore.Application.Features.UsageBilling.Commands;
using TenantCore.Application.Features.UsageBilling.Services;
using TenantCore.Application.Features.UsageBilling.Validators;

namespace TenantCore.Application.Tests.Features.UsageBilling.Validators;

/// <summary>
/// The wallet and fee commands carry almost nothing the caller can choose — the amount is bounded from
/// configuration and resolved server-side, and only identifiers are accepted elsewhere.
/// </summary>
public class WalletAndFeeCommandValidatorTests
{
    private static readonly IOptions<UsageBillingOptions> Options =
        Microsoft.Extensions.Options.Options.Create(
            new UsageBillingOptions { TopUpMinAmount = 100m, TopUpMaxAmount = 100000m });

    private static CreateWalletTopUpCommand TopUp(
        decimal amount, Guid? applicationId = null, Guid? actingUserId = null)
        => new(applicationId ?? Guid.NewGuid(), amount, actingUserId ?? Guid.NewGuid());

    private static CreateWalletTopUpCommandValidator TopUpValidator() => new(Options);

    [Theory]
    [InlineData(100)]
    [InlineData(5000)]
    [InlineData(100000)]
    [InlineData(100.55)]
    public void TopUpValidator_AmountWithinBounds_Passes(decimal amount)
        => TopUpValidator().Validate(TopUp(amount)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(99.99)]
    [InlineData(100000.01)]
    [InlineData(0)]
    [InlineData(-500)]
    public void TopUpValidator_AmountOutsideBounds_Fails(decimal amount)
        => TopUpValidator().Validate(TopUp(amount)).IsValid.Should().BeFalse();

    [Fact]
    public void TopUpValidator_MoreThanTwoDecimals_Fails()
        => TopUpValidator().Validate(TopUp(500.005m)).IsValid.Should().BeFalse();

    [Fact]
    public void TopUpValidator_EmptyApplicationId_Fails()
        => TopUpValidator().Validate(TopUp(500m, applicationId: Guid.Empty)).IsValid.Should().BeFalse();

    [Fact]
    public void TopUpValidator_EmptyActingUser_Fails()
        => TopUpValidator().Validate(TopUp(500m, actingUserId: Guid.Empty)).IsValid.Should().BeFalse();

    [Fact]
    public void CheckTopUpValidator_EveryIdentifierEmptyOrValid_Behaves()
    {
        var validator = new CheckWalletTopUpCommandValidator();

        validator.Validate(new CheckWalletTopUpCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new CheckWalletTopUpCommand(Guid.Empty, Guid.NewGuid())).IsValid.Should().BeFalse();
        validator.Validate(new CheckWalletTopUpCommand(Guid.NewGuid(), Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RegistrationFeePaymentValidator_OnlyIdentifiers_Behaves()
    {
        var validator = new CreateRegistrationFeePaymentCommandValidator();

        validator.Validate(new CreateRegistrationFeePaymentCommand(Guid.NewGuid(), Guid.NewGuid()))
            .IsValid.Should().BeTrue();
        validator.Validate(new CreateRegistrationFeePaymentCommand(Guid.Empty, Guid.NewGuid()))
            .IsValid.Should().BeFalse();
        validator.Validate(new CreateRegistrationFeePaymentCommand(Guid.NewGuid(), Guid.Empty))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void CheckRegistrationFeePaymentValidator_EveryIdentifierEmptyOrValid_Behaves()
    {
        var validator = new CheckRegistrationFeePaymentCommandValidator();

        validator.Validate(new CheckRegistrationFeePaymentCommand(Guid.NewGuid(), Guid.NewGuid()))
            .IsValid.Should().BeTrue();
        validator.Validate(new CheckRegistrationFeePaymentCommand(Guid.Empty, Guid.NewGuid()))
            .IsValid.Should().BeFalse();
        validator.Validate(new CheckRegistrationFeePaymentCommand(Guid.NewGuid(), Guid.Empty))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void TopUpValidator_MessageQuotesTheConfiguredBounds()
    {
        var result = TopUpValidator().Validate(TopUp(5m));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("100") && e.ErrorMessage.Contains("100,000"));
    }
}
