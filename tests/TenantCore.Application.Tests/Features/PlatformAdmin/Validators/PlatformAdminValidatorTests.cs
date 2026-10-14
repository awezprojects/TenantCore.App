using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Options;
using TenantCore.Application.Common;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Application.Features.PlatformAdmin.Models;
using TenantCore.Application.Features.PlatformAdmin.Validators;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.PlatformAdmin.Validators;

public class PlatformAdminValidatorTests
{
    private static readonly IOptions<OnboardingOptions> Options =
        Microsoft.Extensions.Options.Options.Create(new OnboardingOptions { MinPaymentAmount = 1m, MaxPaymentAmount = 500000m });

    private static ClinicContact Contact() => new("Sunrise Clinic", "Dr Mehta", "doctor@example.test", "9876543210");

    // ---------- SuspendClinicCommand ----------

    private static SuspendClinicCommand SuspendCommand(string message = "Payment overdue", string email = "admin@example.test", Guid? adminId = null)
        => new(Guid.NewGuid(), message, adminId ?? Guid.NewGuid(), email);

    [Fact]
    public void SuspendValidator_ValidCommand_Passes()
        => new SuspendClinicCommandValidator().Validate(SuspendCommand()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("ab")]
    public void SuspendValidator_MessageTooShortOrEmpty_Fails(string message)
        => new SuspendClinicCommandValidator().Validate(SuspendCommand(message)).IsValid.Should().BeFalse();

    [Fact]
    public void SuspendValidator_MessageAtMaxLength_Passes()
        => new SuspendClinicCommandValidator().Validate(SuspendCommand(new string('x', 500))).IsValid.Should().BeTrue();

    [Fact]
    public void SuspendValidator_MessageOverMaxLength_Fails()
        => new SuspendClinicCommandValidator().Validate(SuspendCommand(new string('x', 501))).IsValid.Should().BeFalse();

    [Fact]
    public void SuspendValidator_EmptyAdminUserId_Fails()
        => new SuspendClinicCommandValidator().Validate(SuspendCommand(adminId: Guid.Empty)).IsValid.Should().BeFalse();

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void SuspendValidator_InvalidAdminEmail_Fails(string email)
        => new SuspendClinicCommandValidator().Validate(SuspendCommand(email: email)).IsValid.Should().BeFalse();

    [Fact]
    public void SuspendValidator_EmptyApplicationId_Fails()
    {
        var command = new SuspendClinicCommand(Guid.Empty, "Payment overdue", Guid.NewGuid(), "admin@example.test");

        new SuspendClinicCommandValidator().Validate(command).IsValid.Should().BeFalse();
    }

    // ---------- AssignPlanPaymentLinkCommand ----------

    private static AssignPlanPaymentLinkCommand AssignCommand(decimal amount = 999m, string? reason = null, ClinicContact? contact = null)
        => new(Guid.NewGuid(), Guid.NewGuid(), amount, reason, contact ?? Contact(), Guid.NewGuid(), "admin@example.test");

    [Fact]
    public void AssignValidator_ValidCommand_Passes()
        => new AssignPlanPaymentLinkCommandValidator(Options).Validate(AssignCommand()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]          // below the gateway minimum
    [InlineData(-1)]
    [InlineData(500001)]     // above the configured maximum
    public void AssignValidator_AmountOutOfRange_Fails(decimal amount)
        => new AssignPlanPaymentLinkCommandValidator(Options).Validate(AssignCommand(amount)).IsValid.Should().BeFalse();

    [Fact]
    public void AssignValidator_AmountWithMoreThanTwoDecimals_Fails()
        => new AssignPlanPaymentLinkCommandValidator(Options).Validate(AssignCommand(999.999m)).IsValid.Should().BeFalse();

    [Fact]
    public void AssignValidator_BlankContactEmail_Fails()
    {
        var command = AssignCommand(contact: new ClinicContact("Sunrise Clinic", "Dr Mehta", "", null));

        new AssignPlanPaymentLinkCommandValidator(Options).Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void AssignValidator_BlankContactName_Fails()
    {
        // A blank recipient name is dead-lettered by the notification consumer without a send
        // attempt, so it must never reach the queue (workspace CLAUDE.md, rule 9).
        var command = AssignCommand(contact: new ClinicContact("Sunrise Clinic", "", "doctor@example.test", null));

        new AssignPlanPaymentLinkCommandValidator(Options).Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void AssignValidator_ReasonOverMaxLength_Fails()
        => new AssignPlanPaymentLinkCommandValidator(Options)
            .Validate(AssignCommand(reason: new string('x', 501))).IsValid.Should().BeFalse();

    // ---------- CreateClinicPlanOfferCommand ----------

    private static CreateClinicPlanOfferCommand OfferCommand(decimal? price = 499m, DateTime? validUntil = null, string? note = "note")
        => new(Guid.NewGuid(), Guid.NewGuid(), price, validUntil, note, Guid.NewGuid(), "admin@example.test");

    [Fact]
    public void OfferValidator_ValidCommand_Passes()
        => new CreateClinicPlanOfferCommandValidator(Options).Validate(OfferCommand()).IsValid.Should().BeTrue();

    [Fact]
    public void OfferValidator_NoPriceOrExpiry_Passes()
        => new CreateClinicPlanOfferCommandValidator(Options)
            .Validate(OfferCommand(price: null, validUntil: null)).IsValid.Should().BeTrue();

    [Fact]
    public void OfferValidator_ExpiryInThePast_Fails()
        => new CreateClinicPlanOfferCommandValidator(Options)
            .Validate(OfferCommand(validUntil: DateTime.UtcNow.AddDays(-1))).IsValid.Should().BeFalse();

    [Fact]
    public void OfferValidator_NoteOverMaxLength_Fails()
        => new CreateClinicPlanOfferCommandValidator(Options)
            .Validate(OfferCommand(note: new string('x', 251))).IsValid.Should().BeFalse();

    [Fact]
    public void OfferValidator_PriceOutOfRange_Fails()
        => new CreateClinicPlanOfferCommandValidator(Options)
            .Validate(OfferCommand(price: 500001m)).IsValid.Should().BeFalse();

    // ---------- Plan catalogue ----------

    private static PlanDetails Details(
        string name = "Clinic Pro", int durationDays = 90, decimal price = 4999m, int displayOrder = 5,
        decimal? fee = null, int? freeTestDays = null)
        => new(name, "desc", durationDays, price, false, true, displayOrder, fee, freeTestDays);

    [Fact]
    public void CreatePlanValidator_ValidCommand_Passes()
        => new CreateSubscriptionPlanCommandValidator(Options)
            .Validate(new CreateSubscriptionPlanCommand(Details(), Guid.NewGuid(), "admin@example.test"))
            .IsValid.Should().BeTrue();

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public void CreatePlanValidator_NameTooShort_Fails(string name)
        => new CreateSubscriptionPlanCommandValidator(Options)
            .Validate(new CreateSubscriptionPlanCommand(Details(name: name), Guid.NewGuid(), "admin@example.test"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void CreatePlanValidator_NameAtMaxLength_Passes()
        => new CreateSubscriptionPlanCommandValidator(Options)
            .Validate(new CreateSubscriptionPlanCommand(Details(name: new string('x', 60)), Guid.NewGuid(), "admin@example.test"))
            .IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(1096)]
    public void CreatePlanValidator_DurationOutOfRange_Fails(int durationDays)
        => new CreateSubscriptionPlanCommandValidator(Options)
            .Validate(new CreateSubscriptionPlanCommand(Details(durationDays: durationDays), Guid.NewGuid(), "admin@example.test"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void CreatePlanValidator_DisplayOrderOutOfRange_Fails()
        => new CreateSubscriptionPlanCommandValidator(Options)
            .Validate(new CreateSubscriptionPlanCommand(Details(displayOrder: 1000), Guid.NewGuid(), "admin@example.test"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void CreatePlanValidator_ZeroPriceIsStructurallyValid_TrialRuleIsEnforcedInTheHandler()
        => new CreateSubscriptionPlanCommandValidator(Options)
            .Validate(new CreateSubscriptionPlanCommand(Details(price: 0m), Guid.NewGuid(), "admin@example.test"))
            .IsValid.Should().BeTrue();

    // ---------- Remaining commands ----------

    [Fact]
    public void CancelUpcomingValidator_EmptyReason_Fails()
        => new CancelUpcomingSubscriptionCommandValidator()
            .Validate(new CancelUpcomingSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid(), "", Guid.NewGuid(), "admin@example.test"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void CancelUpcomingValidator_ValidCommand_Passes()
        => new CancelUpcomingSubscriptionCommandValidator()
            .Validate(new CancelUpcomingSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid(), "Changed their mind", Guid.NewGuid(), "admin@example.test"))
            .IsValid.Should().BeTrue();

    [Fact]
    public void CancelPaymentLinkValidator_EmptyPaymentId_Fails()
        => new CancelClinicPaymentLinkCommandValidator()
            .Validate(new CancelClinicPaymentLinkCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), "admin@example.test"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void GrantValidator_ValidCommand_Passes()
        => new GrantClinicSubscriptionCommandValidator()
            .Validate(new GrantClinicSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid(), "Goodwill", Contact(), Guid.NewGuid(), "admin@example.test"))
            .IsValid.Should().BeTrue();

    [Fact]
    public void GrantValidator_EmptyReason_Fails()
        => new GrantClinicSubscriptionCommandValidator()
            .Validate(new GrantClinicSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid(), "", Contact(), Guid.NewGuid(), "admin@example.test"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void VisibilityValidator_ValidCommand_Passes()
        => new SetClinicPlanVisibilityCommandValidator()
            .Validate(new SetClinicPlanVisibilityCommand(Guid.NewGuid(), true, Guid.NewGuid(), "admin@example.test"))
            .IsValid.Should().BeTrue();

    [Fact]
    public void WithdrawOfferValidator_EmptyOfferId_Fails()
        => new WithdrawClinicPlanOfferCommandValidator()
            .Validate(new WithdrawClinicPlanOfferCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), "admin@example.test"))
            .IsValid.Should().BeFalse();

    // ---------- Billing (registration fee & usage) ----------

    private static SetClinicBillingCommand BillingCommand(
        BillingMode mode = BillingMode.Subscription, bool applyNow = true,
        decimal? fee = null, int? freeTestDays = null, string? reason = "Negotiated")
        => new(Guid.NewGuid(), mode, applyNow, fee, freeTestDays, reason, Guid.NewGuid(), "admin@example.test");

    [Fact]
    public void SetBillingValidator_ValidCommand_Passes()
        => new SetClinicBillingCommandValidator().Validate(BillingCommand()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("ab")]
    public void SetBillingValidator_ReasonMissingOrTooShort_Fails(string reason)
        => new SetClinicBillingCommandValidator().Validate(BillingCommand(reason: reason)).IsValid.Should().BeFalse();

    [Fact]
    public void SetBillingValidator_ReasonRequiredEvenWhenAppliedImmediately()
        => new SetClinicBillingCommandValidator()
            .Validate(BillingCommand(applyNow: true, reason: ""))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SetBillingValidator_ReasonAtMaxLength_Passes()
        => new SetClinicBillingCommandValidator()
            .Validate(BillingCommand(reason: new string('x', 500)))
            .IsValid.Should().BeTrue();

    [Fact]
    public void SetBillingValidator_UndefinedBillingMode_Fails()
        => new SetClinicBillingCommandValidator()
            .Validate(BillingCommand(mode: (BillingMode)99))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SetBillingValidator_NegativeRegistrationFee_Fails()
        => new SetClinicBillingCommandValidator().Validate(BillingCommand(fee: -1m)).IsValid.Should().BeFalse();

    [Fact]
    public void SetBillingValidator_RegistrationFeeWithThreeDecimals_Fails()
        => new SetClinicBillingCommandValidator().Validate(BillingCommand(fee: 10.123m)).IsValid.Should().BeFalse();

    [Fact]
    public void SetBillingValidator_RegistrationFeeWithTwoDecimals_Passes()
        => new SetClinicBillingCommandValidator().Validate(BillingCommand(fee: 499.99m)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(29)]
    [InlineData(91)]
    public void SetBillingValidator_FreeTestDaysOutOfRange_Fails(int days)
        => new SetClinicBillingCommandValidator()
            .Validate(BillingCommand(freeTestDays: days))
            .IsValid.Should().BeFalse();

    [Theory]
    [InlineData(30)]
    [InlineData(90)]
    public void SetBillingValidator_FreeTestDaysAtTheBounds_Pass(int days)
        => new SetClinicBillingCommandValidator()
            .Validate(BillingCommand(freeTestDays: days))
            .IsValid.Should().BeTrue();

    private static SetClinicUsageRatesCommand RatesCommand(params UsageRateSetting[] rates)
        => new(Guid.NewGuid(), rates, "New price", Guid.NewGuid(), "admin@example.test");

    [Fact]
    public void SetUsageRatesValidator_ValidRate_Passes()
        => new SetClinicUsageRatesCommandValidator()
            .Validate(RatesCommand(new UsageRateSetting(BillingItem.OpdRegistration, 5m)))
            .IsValid.Should().BeTrue();

    [Fact]
    public void SetUsageRatesValidator_NoRates_Fails()
        => new SetClinicUsageRatesCommandValidator().Validate(RatesCommand()).IsValid.Should().BeFalse();

    [Fact]
    public void SetUsageRatesValidator_DuplicateItem_Fails()
        => new SetClinicUsageRatesCommandValidator()
            .Validate(RatesCommand(
                new UsageRateSetting(BillingItem.OpdRegistration, 5m),
                new UsageRateSetting(BillingItem.OpdRegistration, 6m)))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SetUsageRatesValidator_RateWithFiveDecimals_Fails()
        => new SetClinicUsageRatesCommandValidator()
            .Validate(RatesCommand(new UsageRateSetting(BillingItem.OpdRegistration, 1.12345m)))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SetUsageRatesValidator_NegativeRate_Fails()
        => new SetClinicUsageRatesCommandValidator()
            .Validate(RatesCommand(new UsageRateSetting(BillingItem.OpdRegistration, -1m)))
            .IsValid.Should().BeFalse();

    [Fact]
    public void WalletTopUpValidator_ValidCommand_Passes()
        => new SendClinicWalletTopUpCommandValidator()
            .Validate(new SendClinicWalletTopUpCommand(
                Guid.NewGuid(), 1000m, "Top up", Guid.NewGuid(), Guid.NewGuid(), "admin@example.test"))
            .IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void WalletTopUpValidator_NonPositiveAmount_Fails(decimal amount)
        => new SendClinicWalletTopUpCommandValidator()
            .Validate(new SendClinicWalletTopUpCommand(
                Guid.NewGuid(), amount, "Top up", null, Guid.NewGuid(), "admin@example.test"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void WalletCreditValidator_MissingReferenceId_Fails()
        => new CreditClinicWalletCommandValidator()
            .Validate(new CreditClinicWalletCommand(
                Guid.NewGuid(), 1000m, "Goodwill", Guid.Empty, Guid.NewGuid(), "admin@example.test"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void WalletCreditValidator_ValidCommand_Passes()
        => new CreditClinicWalletCommandValidator()
            .Validate(new CreditClinicWalletCommand(
                Guid.NewGuid(), 1000m, "Goodwill", Guid.NewGuid(), Guid.NewGuid(), "admin@example.test"))
            .IsValid.Should().BeTrue();

    [Fact]
    public void WaiveFeeValidator_EmptyReason_Fails()
        => new WaiveRegistrationFeeCommandValidator()
            .Validate(new WaiveRegistrationFeeCommand(Guid.NewGuid(), "", Guid.NewGuid(), "admin@example.test"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void WaiveFeeValidator_ValidCommand_Passes()
        => new WaiveRegistrationFeeCommandValidator()
            .Validate(new WaiveRegistrationFeeCommand(Guid.NewGuid(), "Founding clinic", Guid.NewGuid(), "admin@example.test"))
            .IsValid.Should().BeTrue();

    // ---------- Plan catalogue: the Phase-2 catalogue defaults ----------

    [Fact]
    public void CreatePlanValidator_NegativeRegistrationFee_Fails()
        => new CreateSubscriptionPlanCommandValidator(Options)
            .Validate(new CreateSubscriptionPlanCommand(Details(fee: -1m), Guid.NewGuid(), "admin@example.test"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void CreatePlanValidator_RegistrationFeeWithThreeDecimals_Fails()
        => new CreateSubscriptionPlanCommandValidator(Options)
            .Validate(new CreateSubscriptionPlanCommand(Details(fee: 10.123m), Guid.NewGuid(), "admin@example.test"))
            .IsValid.Should().BeFalse();

    [Theory]
    [InlineData(29)]
    [InlineData(91)]
    public void CreatePlanValidator_FreeTestDaysOutOfRange_Fails(int days)
        => new CreateSubscriptionPlanCommandValidator(Options)
            .Validate(new CreateSubscriptionPlanCommand(Details(freeTestDays: days), Guid.NewGuid(), "admin@example.test"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void CreatePlanValidator_FeeAndFreeDaysAtTheBounds_Pass()
        => new CreateSubscriptionPlanCommandValidator(Options)
            .Validate(new CreateSubscriptionPlanCommand(Details(fee: 500m, freeTestDays: 30), Guid.NewGuid(), "admin@example.test"))
            .IsValid.Should().BeTrue();
}
