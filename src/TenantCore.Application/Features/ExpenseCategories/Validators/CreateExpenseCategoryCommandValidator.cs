using FluentValidation;
using TenantCore.Application.Features.ExpenseCategories.Commands;

namespace TenantCore.Application.Features.ExpenseCategories.Validators;

public sealed class CreateExpenseCategoryCommandValidator : AbstractValidator<CreateExpenseCategoryCommand>
{
    public CreateExpenseCategoryCommandValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}
