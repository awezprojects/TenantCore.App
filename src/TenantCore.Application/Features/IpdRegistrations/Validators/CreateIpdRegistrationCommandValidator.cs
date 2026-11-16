using FluentValidation;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdRegistrations.Commands;

namespace TenantCore.Application.Features.IpdRegistrations.Validators;

public sealed class CreateIpdRegistrationCommandValidator : AbstractValidator<CreateIpdRegistrationCommand>
{
    public CreateIpdRegistrationCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DoctorUserId).NotEmpty();
        // DoctorName carries the "Dr." title (e.g. "Dr. Smith"), so the plain-person-name
        // allow-list (NameValidationRules, no periods) is too strict here — use the broader
        // script/HTML check instead, same as any other free-text display field.
        RuleFor(x => x.DoctorName).NotEmpty().MaximumLength(200)
            .Must(HtmlSanitizationRules.HasNoScriptInjection).WithMessage("Doctor name must not contain script or HTML markup.");
        RuleFor(x => x.BedId).NotEqual(Guid.Empty).When(x => x.BedId.HasValue);
        RuleFor(x => x.InitialFee).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AdmissionNotes)
            .MaximumLength(1000)
            .Must(HtmlSanitizationRules.HasNoScriptInjection).WithMessage("Admission notes must not contain script or HTML markup.")
            .Must(HtmlSanitizationRules.HasNoLinks).WithMessage("Admission notes must not contain links.")
            .When(x => !string.IsNullOrEmpty(x.AdmissionNotes));
    }
}
