using FluentValidation;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdRegistrations.Commands;

namespace TenantCore.Application.Features.IpdRegistrations.Validators;

public sealed class TransferBedCommandValidator : AbstractValidator<TransferBedCommand>
{
    public TransferBedCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.NewBedId).NotEmpty();
        RuleFor(x => x.TransferredByUserId).NotEmpty();
        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .Must(HtmlSanitizationRules.HasNoScriptInjection).WithMessage("Notes must not contain script or HTML markup.")
            .Must(HtmlSanitizationRules.HasNoLinks).WithMessage("Notes must not contain links.")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        // DEF-IPD-16: the exact 0..standard-charge cap can only be checked in the handler (it
        // needs the current room's rate and the clinic's settings) — this only enforces the part
        // that's true regardless of context: a non-negative amount, and a reason whenever an
        // amount is entered, including a deliberate ₹0.
        RuleFor(x => x.ManualChargeAmount).GreaterThanOrEqualTo(0m).When(x => x.ManualChargeAmount.HasValue);
        RuleFor(x => x.ManualChargeReason)
            .NotEmpty().WithMessage("A reason is required when entering a manual charge amount.")
            .MaximumLength(500)
            .Must(SafeTextValidationRules.IsSafeText).WithMessage("Reason must not contain script, HTML markup or links.")
            .When(x => x.ManualChargeAmount.HasValue);
    }
}
