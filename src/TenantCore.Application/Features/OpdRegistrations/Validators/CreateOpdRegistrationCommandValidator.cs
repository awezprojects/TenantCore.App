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
    }
}
