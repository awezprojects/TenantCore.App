using FluentValidation;
using TenantCore.Application.Features.OpdParticulars.Commands;

namespace TenantCore.Application.Features.OpdParticulars.Validators;

public sealed class AddOpdParticularCommandValidator : AbstractValidator<AddOpdParticularCommand>
{
    public AddOpdParticularCommandValidator()
    {
        RuleFor(x => x.Request.OpdRegistrationId).NotEmpty();
        RuleFor(x => x.Request.ParticularId).NotEmpty();
        RuleFor(x => x.Request.Amount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}
