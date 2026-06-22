using FluentValidation;
using TenantCore.Application.Features.DoctorFeeConfigs.Commands;

namespace TenantCore.Application.Features.DoctorFeeConfigs.Validators;

public sealed class CreateDoctorFeeConfigCommandValidator : AbstractValidator<CreateDoctorFeeConfigCommand>
{
    public CreateDoctorFeeConfigCommandValidator()
    {
        RuleFor(x => x.Request.DoctorProfileId).NotEmpty();
        RuleFor(x => x.Request.VisitFee).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}
