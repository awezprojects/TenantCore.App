using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Options;
using TenantCore.Application.Common;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Application.Features.PlatformAdmin.Models;
using TenantCore.Application.Features.PlatformAdmin.Validators;

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

    private static PlanDetails Details(string name = "Clinic Pro", int durationDays = 90, decimal price = 4999m, int displayOrder = 5)
        => new(name, "desc", durationDays, price, false, true, displayOrder);

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
}
