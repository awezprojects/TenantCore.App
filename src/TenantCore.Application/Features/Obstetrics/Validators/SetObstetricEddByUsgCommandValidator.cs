using FluentValidation;
using TenantCore.Application.Features.Obstetrics.Commands;

namespace TenantCore.Application.Features.Obstetrics.Validators;

public sealed class SetObstetricEddByUsgCommandValidator : AbstractValidator<SetObstetricEddByUsgCommand>
{
    public SetObstetricEddByUsgCommandValidator()
    {
        RuleFor(x => x.PrescriptionId).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.Request.EddByUsg).Must(d => d >= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("EDD by USG cannot be in the past.");
    }
}
