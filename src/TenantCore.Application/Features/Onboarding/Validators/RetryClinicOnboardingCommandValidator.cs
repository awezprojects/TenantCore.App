using FluentValidation;
using TenantCore.Application.Features.Onboarding.Commands;

namespace TenantCore.Application.Features.Onboarding.Validators;

public sealed class RetryClinicOnboardingCommandValidator : AbstractValidator<RetryClinicOnboardingCommand>
{
    public RetryClinicOnboardingCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ClinicCode).Length(3, 20).Matches("^[A-Za-z0-9-]+$")
            .When(x => !string.IsNullOrWhiteSpace(x.ClinicCode));
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress();
    }
}
