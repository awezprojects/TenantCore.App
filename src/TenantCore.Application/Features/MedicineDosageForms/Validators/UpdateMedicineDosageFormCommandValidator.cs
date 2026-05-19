using FluentValidation;
using TenantCore.Application.Features.MedicineDosageForms.Commands;

namespace TenantCore.Application.Features.MedicineDosageForms.Validators;

public sealed class UpdateMedicineDosageFormCommandValidator : AbstractValidator<UpdateMedicineDosageFormCommand>
{
    public UpdateMedicineDosageFormCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}
