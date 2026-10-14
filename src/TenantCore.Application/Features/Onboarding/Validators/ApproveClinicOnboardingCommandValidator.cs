using FluentValidation;
using TenantCore.Application.Features.Onboarding.Commands;

namespace TenantCore.Application.Features.Onboarding.Validators;

public sealed class ApproveClinicOnboardingCommandValidator : AbstractValidator<ApproveClinicOnboardingCommand>
{
    public ApproveClinicOnboardingCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.ReviewNote).MaximumLength(1000);
        RuleFor(x => x.ClinicCode).Length(3, 20).Matches("^[A-Za-z0-9-]+$")
            .When(x => !string.IsNullOrWhiteSpace(x.ClinicCode));

        // Billing intent field rules (apply regardless of mode)
        RuleFor(x => x.FreeTestDaysOverride)
            .InclusiveBetween(30, 90).WithMessage("Free test days must be between 30 and 90.")
            .When(x => x.FreeTestDaysOverride.HasValue);

        RuleFor(x => x.RegistrationFeeOverride)
            .GreaterThanOrEqualTo(0m).WithMessage("Registration fee cannot be negative.")
            .Must(f => f == null || decimal.Round(f.Value, 2) == f.Value).WithMessage("Registration fee can have at most 2 decimal places.")
            .When(x => x.RegistrationFeeOverride.HasValue);

        RuleFor(x => x.PayAsYouGoOpdRate)
            .GreaterThanOrEqualTo(0m)
            .Must(v => v == null || decimal.Round(v.Value, 4) == v.Value).WithMessage("OPD rate can have at most 4 decimal places.")
            .When(x => x.PayAsYouGoOpdRate.HasValue);

        RuleFor(x => x.PayAsYouGoIpdAdmissionRate)
            .GreaterThanOrEqualTo(0m)
            .Must(v => v == null || decimal.Round(v.Value, 4) == v.Value).WithMessage("IPD admission rate can have at most 4 decimal places.")
            .When(x => x.PayAsYouGoIpdAdmissionRate.HasValue);

        RuleFor(x => x.PayAsYouGoOtSurgeryRate)
            .GreaterThanOrEqualTo(0m)
            .Must(v => v == null || decimal.Round(v.Value, 4) == v.Value).WithMessage("OT surgery rate can have at most 4 decimal places.")
            .When(x => x.PayAsYouGoOtSurgeryRate.HasValue);

        RuleFor(x => x.PayAsYouGoIpdDischargeRate)
            .GreaterThanOrEqualTo(0m)
            .Must(v => v == null || decimal.Round(v.Value, 4) == v.Value).WithMessage("IPD discharge rate can have at most 4 decimal places.")
            .When(x => x.PayAsYouGoIpdDischargeRate.HasValue);

        RuleFor(x => x.PayAsYouGoInitialWalletCredit)
            .GreaterThanOrEqualTo(0m).WithMessage("Initial wallet credit cannot be negative.")
            .When(x => x.PayAsYouGoInitialWalletCredit.HasValue);
    }
}
