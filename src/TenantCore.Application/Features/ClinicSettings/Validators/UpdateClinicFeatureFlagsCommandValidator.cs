using FluentValidation;
using TenantCore.Application.Features.ClinicSettings.Commands;

namespace TenantCore.Application.Features.ClinicSettings.Validators;

public sealed class UpdateClinicFeatureFlagsCommandValidator : AbstractValidator<UpdateClinicFeatureFlagsCommand>
{
    public UpdateClinicFeatureFlagsCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}
