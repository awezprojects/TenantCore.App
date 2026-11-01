using FluentValidation;
using TenantCore.Application.Features.Onboarding.Commands;

namespace TenantCore.Application.Features.Onboarding.Validators;

public sealed class RejectClinicOnboardingCommandValidator : AbstractValidator<RejectClinicOnboardingCommand>
{
    public RejectClinicOnboardingCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress();
    }
}
