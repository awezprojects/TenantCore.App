using FluentValidation;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdRegistrations.Commands;

namespace TenantCore.Application.Features.IpdRegistrations.Validators;

public sealed class DischargePatientCommandValidator : AbstractValidator<DischargePatientCommand>
{
    public DischargePatientCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.DischargeNotes)
            .MaximumLength(1000)
            .Must(HtmlSanitizationRules.HasNoScriptInjection).WithMessage("Discharge notes must not contain script or HTML markup.")
            .Must(HtmlSanitizationRules.HasNoLinks).WithMessage("Discharge notes must not contain links.")
            .When(x => !string.IsNullOrEmpty(x.DischargeNotes));
    }
}
