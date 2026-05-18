using FluentValidation;
using TenantCore.Application.Features.Prescriptions.Commands;

namespace TenantCore.Application.Features.Prescriptions.Validators;

public sealed class UpdatePrescriptionCommandValidator : AbstractValidator<UpdatePrescriptionCommand>
{
    public UpdatePrescriptionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one medicine is required.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.MedicineId).NotEmpty();
            item.RuleFor(i => i.MedicineName).NotEmpty().MaximumLength(300);
            item.RuleFor(i => i.DosageUnit).NotEmpty().MaximumLength(20);
            item.RuleFor(i => i.DurationDays).GreaterThan(0);
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });
    }
}
