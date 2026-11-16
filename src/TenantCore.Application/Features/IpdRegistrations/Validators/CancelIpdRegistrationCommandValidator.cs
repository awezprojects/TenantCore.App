using FluentValidation;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdRegistrations.Commands;

namespace TenantCore.Application.Features.IpdRegistrations.Validators;

public sealed class CancelIpdRegistrationCommandValidator : AbstractValidator<CancelIpdRegistrationCommand>
{
    public CancelIpdRegistrationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .Must(SafeTextValidationRules.IsSafeText).WithMessage("Reason contains characters that are not allowed.")
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
}
