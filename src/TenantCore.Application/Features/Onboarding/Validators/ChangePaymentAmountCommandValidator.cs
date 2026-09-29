using FluentValidation;
using Microsoft.Extensions.Options;
using TenantCore.Application.Common;
using TenantCore.Application.Features.Onboarding.Commands;

namespace TenantCore.Application.Features.Onboarding.Validators;

public sealed class ChangePaymentAmountCommandValidator : AbstractValidator<ChangePaymentAmountCommand>
{
    public ChangePaymentAmountCommandValidator(IOptions<OnboardingOptions> options)
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(options.Value.MinPaymentAmount)
            .LessThanOrEqualTo(options.Value.MaxPaymentAmount)
            .Must(a => decimal.Round(a, 2) == a).WithMessage("Amount can have at most 2 decimal places.");
        RuleFor(x => x.Reason).NotEmpty().Length(3, 500);
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress();
    }
}
