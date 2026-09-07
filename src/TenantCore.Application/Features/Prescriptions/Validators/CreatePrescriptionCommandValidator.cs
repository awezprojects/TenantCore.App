using FluentValidation;
using TenantCore.Application.Common;
using TenantCore.Application.Features.Prescriptions.Commands;

namespace TenantCore.Application.Features.Prescriptions.Validators;

public sealed class CreatePrescriptionCommandValidator : AbstractValidator<CreatePrescriptionCommand>
{
    public CreatePrescriptionCommandValidator()
    {
        RuleFor(x => x.OpdRegistrationId).NotEmpty();
        RuleFor(x => x.DoctorUserId).NotEmpty();
        RuleFor(x => x.DoctorName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one medicine is required.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.MedicineId).NotEmpty();
            item.RuleFor(i => i.MedicineName).NotEmpty().MaximumLength(300);
            item.RuleFor(i => i.DosageUnit).NotEmpty().MaximumLength(20);
            item.RuleFor(i => i.DurationDays).GreaterThan(0);
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.DosageMorning).GreaterThanOrEqualTo(0).When(i => i.DosageMorning.HasValue);
            item.RuleFor(i => i.DosageAfternoon).GreaterThanOrEqualTo(0).When(i => i.DosageAfternoon.HasValue);
            item.RuleFor(i => i.DosageEvening).GreaterThanOrEqualTo(0).When(i => i.DosageEvening.HasValue);
            item.RuleFor(i => i.DosageNight).GreaterThanOrEqualTo(0).When(i => i.DosageNight.HasValue);
        });

        RuleFor(x => x.NextVisitDate!.Value)
            .Must(DateValidationRules.IsValidNextVisitDate)
            .When(x => x.NextVisitDate.HasValue)
            .WithMessage($"Next visit date must be between today and {DateValidationRules.DefaultFutureLimitDays} days from now.");

        RuleFor(x => x.VitalPulse).GreaterThanOrEqualTo(0).When(x => x.VitalPulse.HasValue);
        RuleFor(x => x.VitalTemp).GreaterThanOrEqualTo(0).When(x => x.VitalTemp.HasValue);
        RuleFor(x => x.VitalWeight).GreaterThanOrEqualTo(0).When(x => x.VitalWeight.HasValue);
        RuleFor(x => x.VitalSpO2).GreaterThanOrEqualTo(0).When(x => x.VitalSpO2.HasValue);
        RuleFor(x => x.VitalRR).GreaterThanOrEqualTo(0).When(x => x.VitalRR.HasValue);
        RuleFor(x => x.VitalSugar).GreaterThanOrEqualTo(0).When(x => x.VitalSugar.HasValue);

        When(x => x.ObstetricData is not null, () =>
        {
            RuleFor(x => x.ObstetricData!.Gravida)
                .GreaterThanOrEqualTo(0).When(x => x.ObstetricData!.Gravida.HasValue);
            RuleFor(x => x.ObstetricData!.Para)
                .GreaterThanOrEqualTo(0).When(x => x.ObstetricData!.Para.HasValue);
            RuleFor(x => x.ObstetricData!.Live)
                .GreaterThanOrEqualTo(0).When(x => x.ObstetricData!.Live.HasValue);
            RuleFor(x => x.ObstetricData!.Abortion)
                .GreaterThanOrEqualTo(0).When(x => x.ObstetricData!.Abortion.HasValue);
            RuleFor(x => x.ObstetricData!.Lmp!.Value)
                .Must(DateValidationRules.IsValidLmp)
                .When(x => x.ObstetricData!.Lmp.HasValue)
                .WithMessage($"LMP date must not be in the future and not more than {DateValidationRules.LmpMaxAgeMonths} months in the past.");
        });
    }
}
