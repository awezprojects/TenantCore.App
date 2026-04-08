using FluentValidation;
using TenantCore.Application.Features.Tenants.Commands;

namespace TenantCore.Application.Features.Tenants.Validators;

public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Subdomain)
            .NotEmpty().WithMessage("Subdomain is required.")
            .MaximumLength(100).WithMessage("Subdomain must not exceed 100 characters.")
            .Matches(@"^[a-z0-9\-]+$").WithMessage("Subdomain must be lowercase alphanumeric with hyphens only.");

        RuleFor(x => x.ContactEmail)
            .EmailAddress().WithMessage("Contact email must be a valid email address.")
            .When(x => !string.IsNullOrEmpty(x.ContactEmail));

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
