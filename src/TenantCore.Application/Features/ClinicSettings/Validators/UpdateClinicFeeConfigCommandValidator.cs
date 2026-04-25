using FluentValidation;
using TenantCore.Application.Features.ClinicSettings.Commands;

namespace TenantCore.Application.Features.ClinicSettings.Validators;

public sealed class UpdateClinicFeeConfigCommandValidator : AbstractValidator<UpdateClinicFeeConfigCommand>
{
    public UpdateClinicFeeConfigCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.OpdFee).GreaterThanOrEqualTo(0);
    }
}
