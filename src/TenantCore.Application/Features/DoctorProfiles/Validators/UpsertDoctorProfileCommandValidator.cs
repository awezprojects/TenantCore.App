using FluentValidation;
using TenantCore.Application.Features.DoctorProfiles.Commands;

namespace TenantCore.Application.Features.DoctorProfiles.Validators;

public sealed class UpsertDoctorProfileCommandValidator : AbstractValidator<UpsertDoctorProfileCommand>
{
    public UpsertDoctorProfileCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RegistrationNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Specialty).MaximumLength(200).When(x => x.Specialty != null);
        RuleFor(x => x.QualificationDetails).MaximumLength(500).When(x => x.QualificationDetails != null);
    }
}
