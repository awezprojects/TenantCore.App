using FluentValidation;
using Microsoft.Extensions.Options;
using TenantCore.Application.Common;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Application.Features.PlatformAdmin.Models;

namespace TenantCore.Application.Features.PlatformAdmin.Validators;

/// <summary>
/// Shared by create and update. The "a paid plan costs at least the minimum" rule is NOT here:
/// only the handler knows whether the plan being edited is the free Trial.
/// </summary>
internal sealed class PlanDetailsValidator : AbstractValidator<PlanDetails>
{
    public PlanDetailsValidator(decimal maxPrice)
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(3).MaximumLength(60);
        RuleFor(x => x.Description).MaximumLength(250);

        RuleFor(x => x.DurationDays)
            .InclusiveBetween(1, 1095)
            .WithMessage("The duration must be between 1 and 1095 days.");

        RuleFor(x => x.Price)
            .InclusiveBetween(0m, maxPrice)
            .WithMessage($"The price must be between 0 and {maxPrice:N2}.");

        RuleFor(x => x.Price)
            .Must(p => decimal.Round(p, 2) == p)
            .WithMessage("The price can have at most 2 decimal places.");

        RuleFor(x => x.DisplayOrder).InclusiveBetween(0, 999);
    }
}

public sealed class CreateSubscriptionPlanCommandValidator : AbstractValidator<CreateSubscriptionPlanCommand>
{
    public CreateSubscriptionPlanCommandValidator(IOptions<OnboardingOptions> options)
    {
        RuleFor(x => x.Details).NotNull().SetValidator(new PlanDetailsValidator(options.Value.MaxPaymentAmount));
        RuleFor(x => x.AdminUserId).AdminUserIdRules();
        RuleFor(x => x.AdminEmail).AdminEmailRules();
    }
}

public sealed class UpdateSubscriptionPlanCommandValidator : AbstractValidator<UpdateSubscriptionPlanCommand>
{
    public UpdateSubscriptionPlanCommandValidator(IOptions<OnboardingOptions> options)
    {
        RuleFor(x => x.PlanId).NotEmpty();
        RuleFor(x => x.Details).NotNull().SetValidator(new PlanDetailsValidator(options.Value.MaxPaymentAmount));
        RuleFor(x => x.AdminUserId).AdminUserIdRules();
        RuleFor(x => x.AdminEmail).AdminEmailRules();
    }
}

public sealed class SetSubscriptionPlanActiveCommandValidator : AbstractValidator<SetSubscriptionPlanActiveCommand>
{
    public SetSubscriptionPlanActiveCommandValidator()
    {
        RuleFor(x => x.PlanId).NotEmpty();
        RuleFor(x => x.AdminUserId).AdminUserIdRules();
        RuleFor(x => x.AdminEmail).AdminEmailRules();
    }
}
