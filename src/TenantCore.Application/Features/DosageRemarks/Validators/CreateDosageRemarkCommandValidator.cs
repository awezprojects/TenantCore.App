using FluentValidation;
using TenantCore.Application.Features.DosageRemarks.Commands;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.DosageRemarks.Validators;

public sealed class CreateDosageRemarkCommandValidator : AbstractValidator<CreateDosageRemarkCommand>
{
    public CreateDosageRemarkCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.MedicineForm).IsInEnum();
        RuleFor(x => x.RemarkEnglish).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RemarkHindi).MaximumLength(500).When(x => x.RemarkHindi is not null);
        RuleFor(x => x.RemarkMarathi).MaximumLength(500).When(x => x.RemarkMarathi is not null);
    }
}
