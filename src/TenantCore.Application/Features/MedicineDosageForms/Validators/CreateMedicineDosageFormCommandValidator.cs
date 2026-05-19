using FluentValidation;
using TenantCore.Application.Features.MedicineDosageForms.Commands;

namespace TenantCore.Application.Features.MedicineDosageForms.Validators;

public sealed class CreateMedicineDosageFormCommandValidator : AbstractValidator<CreateMedicineDosageFormCommand>
{
    public CreateMedicineDosageFormCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}
