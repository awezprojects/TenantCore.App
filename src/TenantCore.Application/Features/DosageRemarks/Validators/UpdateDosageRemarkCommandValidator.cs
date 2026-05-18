using FluentValidation;
using TenantCore.Application.Features.DosageRemarks.Commands;

namespace TenantCore.Application.Features.DosageRemarks.Validators;

public sealed class UpdateDosageRemarkCommandValidator : AbstractValidator<UpdateDosageRemarkCommand>
{
    public UpdateDosageRemarkCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.MedicineForm).IsInEnum();
        RuleFor(x => x.RemarkEnglish).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RemarkHindi).MaximumLength(500).When(x => x.RemarkHindi is not null);
        RuleFor(x => x.RemarkMarathi).MaximumLength(500).When(x => x.RemarkMarathi is not null);
    }
}
