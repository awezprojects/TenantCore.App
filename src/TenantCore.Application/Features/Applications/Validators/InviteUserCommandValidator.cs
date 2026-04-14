using FluentValidation;
using TenantCore.Application.Features.Applications.Commands;

namespace TenantCore.Application.Features.Applications.Validators;

public class InviteUserCommandValidator : AbstractValidator<InviteUserCommand>
{
    public InviteUserCommandValidator()
    {
        RuleFor(x => x.InvitedBy)
            .NotEmpty().WithMessage("InvitedBy user ID is required.");

        RuleFor(x => x.Request.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.Request.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.Request.EmailId)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

        RuleFor(x => x.Request.ApplicationId)
            .NotEmpty().WithMessage("Application ID is required.");

        RuleFor(x => x.Request.RoleId)
            .NotEmpty().WithMessage("Role ID is required.");
    }
}
