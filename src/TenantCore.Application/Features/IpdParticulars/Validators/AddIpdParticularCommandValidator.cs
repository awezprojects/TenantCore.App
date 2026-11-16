using FluentValidation;
using TenantCore.Application.Features.IpdParticulars.Commands;

namespace TenantCore.Application.Features.IpdParticulars.Validators;

public sealed class AddIpdParticularCommandValidator : AbstractValidator<AddIpdParticularCommand>
{
    public AddIpdParticularCommandValidator()
    {
        RuleFor(x => x.IpdRegistrationId).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.ParticularId).NotEmpty();
        RuleFor(x => x.AddedByUserId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
    }
}
