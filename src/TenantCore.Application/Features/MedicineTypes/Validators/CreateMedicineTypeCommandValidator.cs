using FluentValidation;
using TenantCore.Application.Features.MedicineTypes.Commands;

namespace TenantCore.Application.Features.MedicineTypes.Validators;

public sealed class CreateMedicineTypeCommandValidator : AbstractValidator<CreateMedicineTypeCommand>
{
    public CreateMedicineTypeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}
