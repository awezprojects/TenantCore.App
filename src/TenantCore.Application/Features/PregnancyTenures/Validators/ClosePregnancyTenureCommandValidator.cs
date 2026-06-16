using FluentValidation;
using TenantCore.Application.Features.PregnancyTenures.Commands;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.PregnancyTenures.Validators;

public sealed class ClosePregnancyTenureCommandValidator : AbstractValidator<ClosePregnancyTenureCommand>
{
    public ClosePregnancyTenureCommandValidator()
    {
        RuleFor(x => x.TenureId).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.Request.Outcome)
            .IsInEnum()
            .WithMessage("A valid outcome must be selected.");
        RuleFor(x => x.Request.Notes)
            .MaximumLength(1000)
            .When(x => x.Request.Notes is not null);
    }
}
