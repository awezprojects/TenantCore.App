using FluentValidation;
using TenantCore.Application.Features.VitalPresets.Commands;

namespace TenantCore.Application.Features.VitalPresets.Validators;

public sealed class AddVitalPresetCommandValidator : AbstractValidator<AddVitalPresetCommand>
{
    public AddVitalPresetCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.VitalField).IsInEnum();
        RuleFor(x => x.Value)
            .Must(v => v.Trim().Length is >= 1 and <= 64)
            .WithMessage("Value must be between 1 and 64 characters.");
    }
}
