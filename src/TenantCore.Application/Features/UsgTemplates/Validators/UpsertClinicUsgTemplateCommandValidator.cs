using FluentValidation;
using TenantCore.Application.Features.UsgTemplates.Commands;

namespace TenantCore.Application.Features.UsgTemplates.Validators;

public sealed class UpsertClinicUsgTemplateCommandValidator : AbstractValidator<UpsertClinicUsgTemplateCommand>
{
    public UpsertClinicUsgTemplateCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.Request.Rows).NotEmpty().WithMessage("At least one row is required.");
        RuleForEach(x => x.Request.Rows).ChildRules(row =>
        {
            row.RuleFor(r => r.WeekLabel).NotEmpty().MaximumLength(50);
            row.RuleFor(r => r.LmpDayOffset).GreaterThan(0);
            row.RuleFor(r => r.Activity).NotEmpty().MaximumLength(500);
            row.RuleFor(r => r.Indication).NotEmpty().MaximumLength(500);
        });
    }
}
