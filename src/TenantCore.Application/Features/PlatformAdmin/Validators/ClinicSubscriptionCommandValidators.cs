using FluentValidation;
using Microsoft.Extensions.Options;
using TenantCore.Application.Common;
using TenantCore.Application.Features.PlatformAdmin.Commands;

namespace TenantCore.Application.Features.PlatformAdmin.Validators;

public sealed class GrantClinicSubscriptionCommandValidator : AbstractValidator<GrantClinicSubscriptionCommand>
{
    public GrantClinicSubscriptionCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.SubscriptionPlanId).NotEmpty();
        RuleFor(x => x.Reason).ReasonRules("reason");
        RuleFor(x => x.Contact).NotNull().SetValidator(new ClinicContactValidator());
        RuleFor(x => x.AdminUserId).AdminUserIdRules();
        RuleFor(x => x.AdminEmail).AdminEmailRules();
    }
}

public sealed class AssignPlanPaymentLinkCommandValidator : AbstractValidator<AssignPlanPaymentLinkCommand>
{
    public AssignPlanPaymentLinkCommandValidator(IOptions<OnboardingOptions> options)
    {
        var min = options.Value.MinPaymentAmount;
        var max = options.Value.MaxPaymentAmount;

        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.SubscriptionPlanId).NotEmpty();

        RuleFor(x => x.Amount)
            .InclusiveBetween(min, max)
            .WithMessage($"The amount must be between {min:N2} and {max:N2}.");

        RuleFor(x => x.Amount)
            .Must(a => decimal.Round(a, 2) == a)
            .WithMessage("The amount can have at most 2 decimal places.");

        // Whether a reason is REQUIRED depends on the clinic's effective price, which needs the
        // database — that check lives in the handler. Here we only bound the text.
        RuleFor(x => x.AmountReason).MaximumLength(500);

        RuleFor(x => x.Contact).NotNull().SetValidator(new ClinicContactValidator());
        RuleFor(x => x.AdminUserId).AdminUserIdRules();
        RuleFor(x => x.AdminEmail).AdminEmailRules();
    }
}

public sealed class CancelUpcomingSubscriptionCommandValidator : AbstractValidator<CancelUpcomingSubscriptionCommand>
{
    public CancelUpcomingSubscriptionCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.SubscriptionId).NotEmpty();
        RuleFor(x => x.Reason).ReasonRules("reason");
        RuleFor(x => x.AdminUserId).AdminUserIdRules();
        RuleFor(x => x.AdminEmail).AdminEmailRules();
    }
}

public sealed class CancelClinicPaymentLinkCommandValidator : AbstractValidator<CancelClinicPaymentLinkCommand>
{
    public CancelClinicPaymentLinkCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.AdminUserId).AdminUserIdRules();
        RuleFor(x => x.AdminEmail).AdminEmailRules();
    }
}
