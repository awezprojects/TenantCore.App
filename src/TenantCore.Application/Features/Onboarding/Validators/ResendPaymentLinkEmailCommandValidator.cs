using FluentValidation;
using TenantCore.Application.Features.Onboarding.Commands;

namespace TenantCore.Application.Features.Onboarding.Validators;

public sealed class ResendPaymentLinkEmailCommandValidator : AbstractValidator<ResendPaymentLinkEmailCommand>
{
    public ResendPaymentLinkEmailCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress();
    }
}
