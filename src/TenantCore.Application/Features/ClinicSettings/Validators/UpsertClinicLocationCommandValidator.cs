using FluentValidation;
using TenantCore.Application.Features.ClinicSettings.Commands;

namespace TenantCore.Application.Features.ClinicSettings.Validators;

public sealed class UpsertClinicLocationCommandValidator : AbstractValidator<UpsertClinicLocationCommand>
{
    public UpsertClinicLocationCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.StateId).NotEmpty().WithMessage("State is required.");
        RuleFor(x => x.CityId).NotEmpty().WithMessage("City is required.");
    }
}
