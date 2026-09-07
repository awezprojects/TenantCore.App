using FluentValidation;
using TenantCore.Application.Features.OpdRegistrations.Commands;

namespace TenantCore.Application.Features.OpdRegistrations.Validators;

public sealed class CreateOpdRegistrationCommandValidator : AbstractValidator<CreateOpdRegistrationCommand>
{
    public CreateOpdRegistrationCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DoctorUserId).NotEmpty();
        RuleFor(x => x.DoctorName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Fee).GreaterThanOrEqualTo(0).When(x => x.Fee.HasValue);
        RuleFor(x => x.Weight).GreaterThanOrEqualTo(0).When(x => x.Weight.HasValue);
        RuleFor(x => x.PulseRate).GreaterThanOrEqualTo(0).When(x => x.PulseRate.HasValue);
        RuleFor(x => x.OxygenSaturation).GreaterThanOrEqualTo(0).When(x => x.OxygenSaturation.HasValue);
        RuleFor(x => x.Temperature).GreaterThanOrEqualTo(0).When(x => x.Temperature.HasValue);
        RuleFor(x => x.RespiratoryRate).GreaterThanOrEqualTo(0).When(x => x.RespiratoryRate.HasValue);
        RuleFor(x => x.Sugar).GreaterThanOrEqualTo(0).When(x => x.Sugar.HasValue);
    }
}
