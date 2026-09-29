using FluentValidation;
using Microsoft.Extensions.Options;
using TenantCore.Application.Common;
using TenantCore.Application.Features.Onboarding.Commands;

namespace TenantCore.Application.Features.Onboarding.Validators;

public sealed class ApproveClinicOnboardingCommandValidator : AbstractValidator<ApproveClinicOnboardingCommand>
{
    public ApproveClinicOnboardingCommandValidator(IOptions<OnboardingOptions> options)
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.ReviewNote).MaximumLength(1000);
        RuleFor(x => x.AmountReason).MaximumLength(500);
        RuleFor(x => x.ClinicCode).Length(3, 20).Matches("^[A-Za-z0-9-]+$")
            .When(x => !string.IsNullOrWhiteSpace(x.ClinicCode));

        When(x => !x.GrantTrial, () =>
        {
            RuleFor(x => x.SubscriptionPlanId).NotEmpty().WithMessage("A plan is required unless granting a trial.");
            // NotNull is unconditional so a missing Amount is always reported — the range/decimal
            // checks below are guarded separately so they never dereference a null value.
            RuleFor(x => x.Amount).NotNull().WithMessage("Amount is required for a paid plan.");
            RuleFor(x => x.Amount)
                .GreaterThanOrEqualTo(options.Value.MinPaymentAmount)
                .LessThanOrEqualTo(options.Value.MaxPaymentAmount)
                .Must(a => decimal.Round(a!.Value, 2) == a.Value).WithMessage("Amount can have at most 2 decimal places.")
                .When(x => x.Amount.HasValue);
        });
    }
}
