using FluentValidation;
using TenantCore.Application.Features.IpdRegistrations.Commands;

namespace TenantCore.Application.Features.IpdRegistrations.Validators;

public sealed class CreateIpdRegistrationCommandValidator : AbstractValidator<CreateIpdRegistrationCommand>
{
    public CreateIpdRegistrationCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DoctorUserId).NotEmpty();
        RuleFor(x => x.DoctorName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.InitialFee).GreaterThanOrEqualTo(0);
        RuleFor(x => x.WardName).MaximumLength(100).When(x => !string.IsNullOrEmpty(x.WardName));
        RuleFor(x => x.RoomNumber).MaximumLength(20).When(x => !string.IsNullOrEmpty(x.RoomNumber));
        RuleFor(x => x.BedNumber).MaximumLength(20).When(x => !string.IsNullOrEmpty(x.BedNumber));
    }
}
