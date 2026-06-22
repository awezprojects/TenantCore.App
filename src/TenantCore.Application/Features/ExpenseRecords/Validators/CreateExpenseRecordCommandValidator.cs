using FluentValidation;
using TenantCore.Application.Features.ExpenseRecords.Commands;

namespace TenantCore.Application.Features.ExpenseRecords.Validators;

public sealed class CreateExpenseRecordCommandValidator : AbstractValidator<CreateExpenseRecordCommand>
{
    public CreateExpenseRecordCommandValidator()
    {
        RuleFor(x => x.Request.ExpenseCategoryId).NotEmpty();
        RuleFor(x => x.Request.Amount).GreaterThan(0);
        RuleFor(x => x.Request.Notes).MaximumLength(500).When(x => x.Request.Notes is not null);
        RuleFor(x => x.RecordedByUserId).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}
