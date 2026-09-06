using FluentValidation;
using TenantCore.Application.Features.Obstetrics.Commands;

namespace TenantCore.Application.Features.Obstetrics.Validators;

public sealed class ClearObstetricLmpCommandValidator : AbstractValidator<ClearObstetricLmpCommand>
{
    public ClearObstetricLmpCommandValidator()
    {
        RuleFor(x => x.PrescriptionId).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}
