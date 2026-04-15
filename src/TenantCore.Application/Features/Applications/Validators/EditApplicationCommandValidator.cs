using FluentValidation;
using TenantCore.Application.Features.Applications.Commands;

namespace TenantCore.Application.Features.Applications.Validators;

public class EditApplicationCommandValidator : AbstractValidator<EditApplicationCommand>
{
    public EditApplicationCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("Application ID is required.");

        RuleFor(x => x.Request.ApplicationName)
            .NotEmpty().WithMessage("Application name is required.")
            .MaximumLength(200).WithMessage("Application name must not exceed 200 characters.");

        RuleFor(x => x.Request.ApplicationCode)
            .NotEmpty().WithMessage("Application code is required.")
            .MaximumLength(50).WithMessage("Application code must not exceed 50 characters.");

        RuleFor(x => x.Request.ApplicationType)
            .InclusiveBetween(0, 2).WithMessage("Application type must be 0 (Clinic), 1 (School), or 2 (HR Management).");

        RuleFor(x => x.Request.OfficialEmail)
            .EmailAddress().WithMessage("Official email must be a valid email address.")
            .When(x => !string.IsNullOrEmpty(x.Request.OfficialEmail));
    }
}
