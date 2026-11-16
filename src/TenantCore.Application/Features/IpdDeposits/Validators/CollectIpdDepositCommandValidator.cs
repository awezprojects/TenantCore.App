using FluentValidation;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdDeposits.Commands;

namespace TenantCore.Application.Features.IpdDeposits.Validators;

public sealed class CollectIpdDepositCommandValidator : AbstractValidator<CollectIpdDepositCommand>
{
    public CollectIpdDepositCommandValidator()
    {
        RuleFor(x => x.IpdRegistrationId).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.CollectedByUserId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .Must(HtmlSanitizationRules.HasNoScriptInjection).WithMessage("Notes must not contain script or HTML markup.")
            .Must(HtmlSanitizationRules.HasNoLinks).WithMessage("Notes must not contain links.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
