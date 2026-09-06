using FluentValidation;
using TenantCore.Application.Features.MedicineBundles.Commands;

namespace TenantCore.Application.Features.MedicineBundles.Validators;

public sealed class CreateMedicineBundleCommandValidator : AbstractValidator<CreateMedicineBundleCommand>
{
    public CreateMedicineBundleCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.Request.DoctorUserId).NotEmpty();
        RuleFor(x => x.Request.DoctorName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.DurationDays).GreaterThan(0);
        RuleFor(x => x.Request.Notes).MaximumLength(1000);
        RuleFor(x => x.Request.Items).NotEmpty().WithMessage("A bundle must contain at least one medicine.");

        RuleForEach(x => x.Request.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.MedicineId).NotEmpty();
            item.RuleFor(i => i.MedicineName).NotEmpty().MaximumLength(300);
            item.RuleFor(i => i.DosageUnit).NotEmpty().MaximumLength(20);
            item.RuleFor(i => i.DurationDays).GreaterThan(0);
        });
    }
}
