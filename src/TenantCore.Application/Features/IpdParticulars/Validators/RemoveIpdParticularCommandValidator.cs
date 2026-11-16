using FluentValidation;
using TenantCore.Application.Features.IpdParticulars.Commands;

namespace TenantCore.Application.Features.IpdParticulars.Validators;

public sealed class RemoveIpdParticularCommandValidator : AbstractValidator<RemoveIpdParticularCommand>
{
    public RemoveIpdParticularCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}
