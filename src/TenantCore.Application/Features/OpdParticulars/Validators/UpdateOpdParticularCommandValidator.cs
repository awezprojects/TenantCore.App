using FluentValidation;
using TenantCore.Application.Features.OpdParticulars.Commands;

namespace TenantCore.Application.Features.OpdParticulars.Validators;

public sealed class UpdateOpdParticularCommandValidator : AbstractValidator<UpdateOpdParticularCommand>
{
    public UpdateOpdParticularCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.Amount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}
