using FluentValidation;
using TenantCore.Application.Features.Onboarding.Commands;

namespace TenantCore.Application.Features.Onboarding.Validators;

public sealed class CheckOnboardingPaymentCommandValidator : AbstractValidator<CheckOnboardingPaymentCommand>
{
    public CheckOnboardingPaymentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
