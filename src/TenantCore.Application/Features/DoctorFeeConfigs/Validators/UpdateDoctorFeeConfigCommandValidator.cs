using FluentValidation;
using TenantCore.Application.Features.DoctorFeeConfigs.Commands;

namespace TenantCore.Application.Features.DoctorFeeConfigs.Validators;

public sealed class UpdateDoctorFeeConfigCommandValidator : AbstractValidator<UpdateDoctorFeeConfigCommand>
{
    public UpdateDoctorFeeConfigCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.VisitFee).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}
