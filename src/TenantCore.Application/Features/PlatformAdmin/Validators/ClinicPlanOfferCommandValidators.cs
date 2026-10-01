using FluentValidation;
using Microsoft.Extensions.Options;
using TenantCore.Application.Common;
using TenantCore.Application.Features.PlatformAdmin.Commands;

namespace TenantCore.Application.Features.PlatformAdmin.Validators;

public sealed class SetClinicPlanVisibilityCommandValidator : AbstractValidator<SetClinicPlanVisibilityCommand>
{
    public SetClinicPlanVisibilityCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.AdminUserId).AdminUserIdRules();
        RuleFor(x => x.AdminEmail).AdminEmailRules();
    }
}

public sealed class CreateClinicPlanOfferCommandValidator : AbstractValidator<CreateClinicPlanOfferCommand>
{
    public CreateClinicPlanOfferCommandValidator(IOptions<OnboardingOptions> options)
    {
        var min = options.Value.MinPaymentAmount;
        var max = options.Value.MaxPaymentAmount;

        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.SubscriptionPlanId).NotEmpty();

        When(x => x.OfferPrice.HasValue, () =>
        {
            RuleFor(x => x.OfferPrice!.Value)
                .InclusiveBetween(min, max)
                .WithMessage($"The offer price must be between {min:N2} and {max:N2}.");

            RuleFor(x => x.OfferPrice!.Value)
                .Must(p => decimal.Round(p, 2) == p)
                .WithMessage("The offer price can have at most 2 decimal places.");
        });

        When(x => x.ValidUntil.HasValue, () =>
        {
            RuleFor(x => x.ValidUntil!.Value)
                .GreaterThan(_ => DateTime.UtcNow)
                .WithMessage("The offer's expiry must be in the future.");
        });

        RuleFor(x => x.Note).MaximumLength(250);
        RuleFor(x => x.AdminUserId).AdminUserIdRules();
        RuleFor(x => x.AdminEmail).AdminEmailRules();
    }
}

public sealed class WithdrawClinicPlanOfferCommandValidator : AbstractValidator<WithdrawClinicPlanOfferCommand>
{
    public WithdrawClinicPlanOfferCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.OfferId).NotEmpty();
        RuleFor(x => x.AdminUserId).AdminUserIdRules();
        RuleFor(x => x.AdminEmail).AdminEmailRules();
    }
}
