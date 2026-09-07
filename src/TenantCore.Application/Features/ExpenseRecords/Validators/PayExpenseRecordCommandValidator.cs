using FluentValidation;
using TenantCore.Application.Features.ExpenseRecords.Commands;

namespace TenantCore.Application.Features.ExpenseRecords.Validators;

public sealed class PayExpenseRecordCommandValidator : AbstractValidator<PayExpenseRecordCommand>
{
    public PayExpenseRecordCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.PayAmount).GreaterThan(0);
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}
