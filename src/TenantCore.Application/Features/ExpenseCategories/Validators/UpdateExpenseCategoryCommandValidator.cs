using FluentValidation;
using TenantCore.Application.Features.ExpenseCategories.Commands;

namespace TenantCore.Application.Features.ExpenseCategories.Validators;

public sealed class UpdateExpenseCategoryCommandValidator : AbstractValidator<UpdateExpenseCategoryCommand>
{
    public UpdateExpenseCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}
