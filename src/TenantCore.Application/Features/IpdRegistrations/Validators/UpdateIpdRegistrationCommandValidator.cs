using FluentValidation;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdRegistrations.Commands;

namespace TenantCore.Application.Features.IpdRegistrations.Validators;

public sealed class UpdateIpdRegistrationCommandValidator : AbstractValidator<UpdateIpdRegistrationCommand>
{
    public UpdateIpdRegistrationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.DoctorUserId).NotEmpty();
        // See CreateIpdRegistrationCommandValidator — DoctorName carries the "Dr." title, so the
        // plain-person-name allow-list (no periods) is too strict; use the script/HTML check instead.
        RuleFor(x => x.DoctorName).NotEmpty().MaximumLength(200)
            .Must(HtmlSanitizationRules.HasNoScriptInjection).WithMessage("Doctor name must not contain script or HTML markup.");
        RuleFor(x => x.AdmissionNotes)
            .MaximumLength(1000)
            .Must(HtmlSanitizationRules.HasNoScriptInjection).WithMessage("Admission notes must not contain script or HTML markup.")
            .Must(HtmlSanitizationRules.HasNoLinks).WithMessage("Admission notes must not contain links.")
            .When(x => !string.IsNullOrEmpty(x.AdmissionNotes));
    }
}
