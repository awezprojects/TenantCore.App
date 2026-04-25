using FluentValidation;
using TenantCore.Application.Features.MedicineTypes.Commands;

namespace TenantCore.Application.Features.MedicineTypes.Validators;

public sealed class UpdateMedicineTypeCommandValidator : AbstractValidator<UpdateMedicineTypeCommand>
{
    public UpdateMedicineTypeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}
