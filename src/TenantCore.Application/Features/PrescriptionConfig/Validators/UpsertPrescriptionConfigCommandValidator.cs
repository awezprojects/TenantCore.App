using FluentValidation;
using TenantCore.Application.Features.PrescriptionConfig.Commands;

namespace TenantCore.Application.Features.PrescriptionConfig.Validators;

public sealed class UpsertPrescriptionConfigCommandValidator : AbstractValidator<UpsertPrescriptionConfigCommand>
{
    public UpsertPrescriptionConfigCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.DefaultLanguage).IsInEnum();
    }
}
