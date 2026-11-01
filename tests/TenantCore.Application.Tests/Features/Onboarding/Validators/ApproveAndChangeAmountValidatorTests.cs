using FluentAssertions;
using Microsoft.Extensions.Options;
using TenantCore.Application.Common;
using TenantCore.Application.Features.Onboarding.Commands;
using TenantCore.Application.Features.Onboarding.Validators;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Onboarding.Validators;

/// <summary>
/// ApproveClinicOnboardingCommandValidator and ChangePaymentAmountCommandValidator tests.
/// All approvals now provision directly — no plan or amount fields on the approve command.
/// </summary>
public class ApproveAndChangeAmountValidatorTests
{
    private static readonly IOptions<OnboardingOptions> AmountOptions = Microsoft.Extensions.Options.Options.Create(
        new OnboardingOptions { MinPaymentAmount = 1.00m, MaxPaymentAmount = 500000m });

    private readonly ApproveClinicOnboardingCommandValidator _approveValidator = new();
    private readonly ChangePaymentAmountCommandValidator _changeAmountValidator = new(AmountOptions);

    private static ApproveClinicOnboardingCommand BaseApproveCommand(
        bool grantTrial = false, string? clinicCode = null, BillingMode? billingMode = null,
        decimal? registrationFee = null, int? freeTestDays = null,
        decimal? opdRate = null, decimal? ipdRate = null, decimal? otRate = null,
        decimal? dischargeRate = null, decimal? walletCredit = null) =>
        new(Guid.NewGuid(), grantTrial, clinicCode, null, Guid.NewGuid(), "admin@example.test",
            BillingModeOverride: billingMode,
            RegistrationFeeOverride: registrationFee,
            FreeTestDaysOverride: freeTestDays,
            PayAsYouGoOpdRate: opdRate,
            PayAsYouGoIpdAdmissionRate: ipdRate,
            PayAsYouGoOtSurgeryRate: otRate,
            PayAsYouGoIpdDischargeRate: dischargeRate,
            PayAsYouGoInitialWalletCredit: walletCredit);

    private static ChangePaymentAmountCommand ChangeAmountCommand(decimal amount, string reason = "a valid reason") =>
        new(Guid.NewGuid(), amount, reason, Guid.NewGuid(), "admin@example.test");

    // ---- core fields ---

    [Fact]
    public void Approve_EmptyId_ReturnsError()
    {
        var command = new ApproveClinicOnboardingCommand(Guid.Empty, false, null, null, Guid.NewGuid(), "admin@example.test");
        _approveValidator.Validate(command).Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public void Approve_EmptyAdminUserId_ReturnsError()
    {
        var command = new ApproveClinicOnboardingCommand(Guid.NewGuid(), false, null, null, Guid.Empty, "admin@example.test");
        _approveValidator.Validate(command).Errors.Should().Contain(e => e.PropertyName == "AdminUserId");
    }

    [Fact]
    public void Approve_InvalidAdminEmail_ReturnsError()
    {
        var command = new ApproveClinicOnboardingCommand(Guid.NewGuid(), false, null, null, Guid.NewGuid(), "not-an-email");
        _approveValidator.Validate(command).Errors.Should().Contain(e => e.PropertyName == "AdminEmail");
    }

    [Fact]
    public void Approve_ValidSubscriptionApproval_PassesValidation()
    {
        var result = _approveValidator.Validate(BaseApproveCommand(billingMode: BillingMode.Subscription, registrationFee: 500m, freeTestDays: 30));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Approve_ValidTrialGrant_PassesValidation()
    {
        var result = _approveValidator.Validate(BaseApproveCommand(grantTrial: true));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Approve_ValidPayAsYouGo_PassesValidation()
    {
        var result = _approveValidator.Validate(BaseApproveCommand(billingMode: BillingMode.PayAsYouGo, opdRate: 10m));
        result.IsValid.Should().BeTrue();
    }

    // ---- clinic code ---

    [Theory]
    [InlineData("AB")]                        // too short
    [InlineData("ABCDEFGHIJKLMNOPQRSTU")]     // too long (21)
    public void Approve_ClinicCodeOverrideOutsideLengthRange_ReturnsError(string code)
    {
        var result = _approveValidator.Validate(BaseApproveCommand(clinicCode: code));
        result.Errors.Should().Contain(e => e.PropertyName == "ClinicCode");
    }

    [Fact]
    public void Approve_ValidClinicCode_PassesValidation()
    {
        var result = _approveValidator.Validate(BaseApproveCommand(clinicCode: "SUNRISE"));
        result.Errors.Should().NotContain(e => e.PropertyName == "ClinicCode");
    }

    // ---- registration fee ---

    [Fact]
    public void Approve_NegativeRegistrationFeeOverride_ReturnsError()
    {
        var result = _approveValidator.Validate(BaseApproveCommand(registrationFee: -1m));
        result.Errors.Should().Contain(e => e.PropertyName == "RegistrationFeeOverride");
    }

    [Fact]
    public void Approve_RegistrationFeeWithThreeDecimalPlaces_ReturnsError()
    {
        var result = _approveValidator.Validate(BaseApproveCommand(registrationFee: 100.123m));
        result.Errors.Should().Contain(e => e.PropertyName == "RegistrationFeeOverride");
    }

    [Fact]
    public void Approve_ZeroRegistrationFee_IsValid()
    {
        var result = _approveValidator.Validate(BaseApproveCommand(registrationFee: 0m));
        result.Errors.Should().NotContain(e => e.PropertyName == "RegistrationFeeOverride");
    }

    // ---- free test days ---

    [Theory]
    [InlineData(29)]
    [InlineData(91)]
    public void Approve_FreeTestDays_OutsideRange_ReturnsError(int days)
    {
        var result = _approveValidator.Validate(BaseApproveCommand(freeTestDays: days));
        result.Errors.Should().Contain(e => e.PropertyName == "FreeTestDaysOverride");
    }

    [Theory]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(90)]
    public void Approve_FreeTestDays_InRange_IsValid(int days)
    {
        var result = _approveValidator.Validate(BaseApproveCommand(freeTestDays: days));
        result.Errors.Should().NotContain(e => e.PropertyName == "FreeTestDaysOverride");
    }

    // ---- PAYG rates ---

    [Fact]
    public void Approve_NegativeOpdRate_ReturnsError()
    {
        var result = _approveValidator.Validate(BaseApproveCommand(billingMode: BillingMode.PayAsYouGo, opdRate: -1m));
        result.Errors.Should().Contain(e => e.PropertyName == "PayAsYouGoOpdRate");
    }

    [Fact]
    public void Approve_OpdRateWith5DecimalPlaces_ReturnsError()
    {
        var result = _approveValidator.Validate(BaseApproveCommand(billingMode: BillingMode.PayAsYouGo, opdRate: 1.12345m));
        result.Errors.Should().Contain(e => e.PropertyName == "PayAsYouGoOpdRate");
    }

    [Fact]
    public void Approve_OpdRateWith4DecimalPlaces_IsValid()
    {
        var result = _approveValidator.Validate(BaseApproveCommand(billingMode: BillingMode.PayAsYouGo, opdRate: 1.1234m));
        result.Errors.Should().NotContain(e => e.PropertyName == "PayAsYouGoOpdRate");
    }

    [Fact]
    public void Approve_NegativeWalletCredit_ReturnsError()
    {
        var result = _approveValidator.Validate(BaseApproveCommand(billingMode: BillingMode.PayAsYouGo, walletCredit: -0.01m));
        result.Errors.Should().Contain(e => e.PropertyName == "PayAsYouGoInitialWalletCredit");
    }

    // ---- ChangePaymentAmount (unchanged behaviour) ---

    [Fact]
    public void ChangeAmount_AmountBelowMinimum_ReturnsError()
    {
        _changeAmountValidator.Validate(ChangeAmountCommand(0.99m)).Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void ChangeAmount_AmountAtMinimum_ReturnsValid()
    {
        _changeAmountValidator.Validate(ChangeAmountCommand(1.00m)).Errors.Should().NotContain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void ChangeAmount_AmountAtMaximum_ReturnsValid()
    {
        _changeAmountValidator.Validate(ChangeAmountCommand(500000m)).Errors.Should().NotContain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void ChangeAmount_AmountAboveMaximum_ReturnsError()
    {
        _changeAmountValidator.Validate(ChangeAmountCommand(500000.01m)).Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void ChangeAmount_AmountWithThreeDecimalPlaces_ReturnsError()
    {
        _changeAmountValidator.Validate(ChangeAmountCommand(100.123m)).Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public void ChangeAmount_ReasonTooShortOrEmpty_ReturnsError(string reason)
    {
        _changeAmountValidator.Validate(ChangeAmountCommand(100m, reason)).Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public void ChangeAmount_ReasonAtMinimumLength_ReturnsValid()
    {
        _changeAmountValidator.Validate(ChangeAmountCommand(100m, "abc")).Errors.Should().NotContain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public void ChangeAmount_ReasonOverMaximumLength_ReturnsError()
    {
        _changeAmountValidator.Validate(ChangeAmountCommand(100m, new string('a', 501))).Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public void ChangeAmount_EmptyAdminEmail_ReturnsError()
    {
        var command = new ChangePaymentAmountCommand(Guid.NewGuid(), 100m, "a valid reason", Guid.NewGuid(), string.Empty);
        _changeAmountValidator.Validate(command).Errors.Should().Contain(e => e.PropertyName == "AdminEmail");
    }
}
