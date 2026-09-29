using FluentAssertions;
using Microsoft.Extensions.Options;
using TenantCore.Application.Common;
using TenantCore.Application.Features.Onboarding.Commands;
using TenantCore.Application.Features.Onboarding.Validators;

namespace TenantCore.Application.Tests.Features.Onboarding.Validators;

/// <summary>
/// Both ApproveClinicOnboardingCommandValidator and ChangePaymentAmountCommandValidator share the
/// same Amount boundary rules, sourced from OnboardingOptions.Min/MaxPaymentAmount — tested here
/// together per the plan's "ApproveAndChangeAmountValidatorTests" grouping.
/// </summary>
public class ApproveAndChangeAmountValidatorTests
{
    private static readonly IOptions<OnboardingOptions> AmountOptions = Microsoft.Extensions.Options.Options.Create(
        new OnboardingOptions { MinPaymentAmount = 1.00m, MaxPaymentAmount = 500000m });

    private readonly ApproveClinicOnboardingCommandValidator _approveValidator = new(AmountOptions);
    private readonly ChangePaymentAmountCommandValidator _changeAmountValidator = new(AmountOptions);

    private static ApproveClinicOnboardingCommand ApproveCommand(decimal? amount, string? amountReason = "reason") =>
        new(Guid.NewGuid(), Guid.NewGuid(), false, amount, amountReason, null, null, Guid.NewGuid(), "admin@example.test");

    private static ChangePaymentAmountCommand ChangeAmountCommand(decimal amount, string reason = "a valid reason") =>
        new(Guid.NewGuid(), amount, reason, Guid.NewGuid(), "admin@example.test");

    [Fact]
    public void Approve_AmountBelowMinimum_ReturnsError()
    {
        var result = _approveValidator.Validate(ApproveCommand(0.99m));
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void Approve_AmountAtMinimum_ReturnsValid()
    {
        var result = _approveValidator.Validate(ApproveCommand(1.00m));
        result.Errors.Should().NotContain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void Approve_AmountAtMaximum_ReturnsValid()
    {
        var result = _approveValidator.Validate(ApproveCommand(500000m));
        result.Errors.Should().NotContain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void Approve_AmountAboveMaximum_ReturnsError()
    {
        var result = _approveValidator.Validate(ApproveCommand(500000.01m));
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void Approve_AmountWithThreeDecimalPlaces_ReturnsError()
    {
        var result = _approveValidator.Validate(ApproveCommand(100.123m));
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void Approve_AmountMissingForPaidPlan_ReturnsError()
    {
        var result = _approveValidator.Validate(ApproveCommand(null));
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void Approve_GrantTrial_AmountAndPlanNotRequired()
    {
        var command = new ApproveClinicOnboardingCommand(Guid.NewGuid(), null, true, null, null, null, null, Guid.NewGuid(), "admin@example.test");
        var result = _approveValidator.Validate(command);
        result.Errors.Should().NotContain(e => e.PropertyName == "Amount" || e.PropertyName == "SubscriptionPlanId");
    }

    [Fact]
    public void Approve_PlanIdEmptyWhenNotGrantingTrial_ReturnsError()
    {
        var command = new ApproveClinicOnboardingCommand(Guid.NewGuid(), null, false, 100m, null, null, null, Guid.NewGuid(), "admin@example.test");
        var result = _approveValidator.Validate(command);
        result.Errors.Should().Contain(e => e.PropertyName == "SubscriptionPlanId");
    }

    [Theory]
    [InlineData("AB")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTU")]
    public void Approve_ClinicCodeOverrideOutsideLengthRange_ReturnsError(string code)
    {
        var command = new ApproveClinicOnboardingCommand(Guid.NewGuid(), Guid.NewGuid(), false, 100m, "reason", code, null, Guid.NewGuid(), "admin@example.test");
        var result = _approveValidator.Validate(command);
        result.Errors.Should().Contain(e => e.PropertyName == "ClinicCode");
    }

    [Fact]
    public void ChangeAmount_AmountBelowMinimum_ReturnsError()
    {
        var result = _changeAmountValidator.Validate(ChangeAmountCommand(0.99m));
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void ChangeAmount_AmountAtMinimum_ReturnsValid()
    {
        var result = _changeAmountValidator.Validate(ChangeAmountCommand(1.00m));
        result.Errors.Should().NotContain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void ChangeAmount_AmountAtMaximum_ReturnsValid()
    {
        var result = _changeAmountValidator.Validate(ChangeAmountCommand(500000m));
        result.Errors.Should().NotContain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void ChangeAmount_AmountAboveMaximum_ReturnsError()
    {
        var result = _changeAmountValidator.Validate(ChangeAmountCommand(500000.01m));
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void ChangeAmount_AmountWithThreeDecimalPlaces_ReturnsError()
    {
        var result = _changeAmountValidator.Validate(ChangeAmountCommand(100.123m));
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]     // 2 chars — below the 3-char minimum
    public void ChangeAmount_ReasonTooShortOrEmpty_ReturnsError(string reason)
    {
        var result = _changeAmountValidator.Validate(ChangeAmountCommand(100m, reason));
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public void ChangeAmount_ReasonAtMinimumLength_ReturnsValid()
    {
        var result = _changeAmountValidator.Validate(ChangeAmountCommand(100m, "abc")); // 3 chars
        result.Errors.Should().NotContain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public void ChangeAmount_ReasonAtMaximumLength_ReturnsValid()
    {
        var result = _changeAmountValidator.Validate(ChangeAmountCommand(100m, new string('a', 500)));
        result.Errors.Should().NotContain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public void ChangeAmount_ReasonOverMaximumLength_ReturnsError()
    {
        var result = _changeAmountValidator.Validate(ChangeAmountCommand(100m, new string('a', 501)));
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public void ChangeAmount_EmptyAdminEmail_ReturnsError()
    {
        var command = new ChangePaymentAmountCommand(Guid.NewGuid(), 100m, "a valid reason", Guid.NewGuid(), string.Empty);
        var result = _changeAmountValidator.Validate(command);
        result.Errors.Should().Contain(e => e.PropertyName == "AdminEmail");
    }
}
