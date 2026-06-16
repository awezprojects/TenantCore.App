using FluentValidation;
using TenantCore.Application.Features.Obstetrics.Commands;

namespace TenantCore.Application.Features.Obstetrics.Validators;

public sealed class SetObstetricLmpCommandValidator : AbstractValidator<SetObstetricLmpCommand>
{
    public SetObstetricLmpCommandValidator()
    {
        RuleFor(x => x.PrescriptionId).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.Request.Lmp).Must(d => d <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("LMP date cannot be in the future.");
    }
}
