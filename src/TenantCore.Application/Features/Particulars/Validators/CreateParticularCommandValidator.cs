using FluentValidation;
using TenantCore.Application.Features.Particulars.Commands;

namespace TenantCore.Application.Features.Particulars.Validators;

public sealed class CreateParticularCommandValidator : AbstractValidator<CreateParticularCommand>
{
    public CreateParticularCommandValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Request.DefaultAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}
