using FluentValidation;
using TenantCore.Application.Features.Medicines.Commands;

namespace TenantCore.Application.Features.Medicines.Validators;

public sealed class UpdateMedicineCommandValidator : AbstractValidator<UpdateMedicineCommand>
{
    public UpdateMedicineCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.GenericName).MaximumLength(200).When(x => x.GenericName is not null);
        RuleFor(x => x.BrandName).MaximumLength(200).When(x => x.BrandName is not null);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
        RuleFor(x => x.Composition).MaximumLength(500).When(x => x.Composition is not null);
        RuleFor(x => x.Composition2).MaximumLength(500).When(x => x.Composition2 is not null);
        RuleFor(x => x.Dosage).MaximumLength(200).When(x => x.Dosage is not null);
        RuleFor(x => x.Form).MaximumLength(50).When(x => x.Form is not null);
        RuleFor(x => x.Manufacturer).MaximumLength(200).When(x => x.Manufacturer is not null);
        RuleFor(x => x.PackSize).MaximumLength(100).When(x => x.PackSize is not null);
        RuleFor(x => x.Uses).MaximumLength(1000).When(x => x.Uses is not null);
        RuleFor(x => x.SideEffects).MaximumLength(1000).When(x => x.SideEffects is not null);
        RuleFor(x => x.Contraindications).MaximumLength(1000).When(x => x.Contraindications is not null);
        RuleFor(x => x.Storage).MaximumLength(200).When(x => x.Storage is not null);
    }
}
