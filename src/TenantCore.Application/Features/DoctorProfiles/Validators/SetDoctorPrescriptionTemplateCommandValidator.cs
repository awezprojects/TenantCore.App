using FluentValidation;
using TenantCore.Application.Features.DoctorProfiles.Commands;

namespace TenantCore.Application.Features.DoctorProfiles.Validators;

public sealed class SetDoctorPrescriptionTemplateCommandValidator : AbstractValidator<SetDoctorPrescriptionTemplateCommand>
{
    public SetDoctorPrescriptionTemplateCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Template).IsInEnum();
    }
}
