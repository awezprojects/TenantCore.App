using FluentValidation;
using TenantCore.Application.Features.OpdRegistrations.Commands;

namespace TenantCore.Application.Features.OpdRegistrations.Validators;

public sealed class UpdateOpdRegistrationCommandValidator : AbstractValidator<UpdateOpdRegistrationCommand>
{
    public UpdateOpdRegistrationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.DoctorUserId).NotEmpty();
        RuleFor(x => x.DoctorName).NotEmpty().MaximumLength(200);
        // DEF-OPD-10: keep the same upper bound as create.
        RuleFor(x => x.Fee).InclusiveBetween(0, 100_000m);
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
