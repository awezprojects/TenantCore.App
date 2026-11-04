using FluentValidation;
using TenantCore.Application.Features.OpdRegistrations.Commands;

namespace TenantCore.Application.Features.OpdRegistrations.Validators;

public sealed class CreateOpdRegistrationCommandValidator : AbstractValidator<CreateOpdRegistrationCommand>
{
    // DEF-OPD-10: a fee with no upper bound let a typo (or a bad-faith entry) auto-collect
    // into the counter session total. One lakh covers any real OPD visit.
    private const decimal MaxFee = 100_000m;

    public CreateOpdRegistrationCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DoctorUserId).NotEmpty();
        RuleFor(x => x.DoctorName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Fee).InclusiveBetween(0, MaxFee).When(x => x.Fee.HasValue);

        // DEF-OPD-11: vitals previously only checked ">= 0", so clinically impossible values
        // (O2 250%, Temp 500°F, Pulse 900bpm, Weight 9999kg, BP "abc") were accepted and stored.
        RuleFor(x => x.Weight).InclusiveBetween(0, 500).When(x => x.Weight.HasValue);
        RuleFor(x => x.PulseRate).InclusiveBetween(0, 300).When(x => x.PulseRate.HasValue);
        RuleFor(x => x.OxygenSaturation).InclusiveBetween(0, 100).When(x => x.OxygenSaturation.HasValue);
        RuleFor(x => x.Temperature).InclusiveBetween(70, 115).When(x => x.Temperature.HasValue);
        RuleFor(x => x.RespiratoryRate).InclusiveBetween(0, 100).When(x => x.RespiratoryRate.HasValue);
        RuleFor(x => x.Sugar).InclusiveBetween(0, 1000).When(x => x.Sugar.HasValue);
        RuleFor(x => x.BloodPressure)
            .Matches(@"^\d{2,3}/\d{2,3}$")
            .WithMessage("Blood pressure must be in the form systolic/diastolic, e.g. 120/80.")
            .When(x => !string.IsNullOrWhiteSpace(x.BloodPressure));
    }
}
