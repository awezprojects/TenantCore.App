using FluentValidation;
using TenantCore.Application.Features.Onboarding.Commands;

namespace TenantCore.Application.Features.Onboarding.Validators;

public sealed class CancelClinicOnboardingCommandValidator : AbstractValidator<CancelClinicOnboardingCommand>
{
    public CancelClinicOnboardingCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
