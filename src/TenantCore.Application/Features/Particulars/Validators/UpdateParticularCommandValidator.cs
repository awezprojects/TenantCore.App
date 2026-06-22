using FluentValidation;
using TenantCore.Application.Features.Particulars.Commands;

namespace TenantCore.Application.Features.Particulars.Validators;

public sealed class UpdateParticularCommandValidator : AbstractValidator<UpdateParticularCommand>
{
    public UpdateParticularCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Request.DefaultAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}
