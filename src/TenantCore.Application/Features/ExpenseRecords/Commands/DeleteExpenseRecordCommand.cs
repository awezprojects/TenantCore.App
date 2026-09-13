using MediatR;
using TenantCore.Application.Common;

namespace TenantCore.Application.Features.ExpenseRecords.Commands;

public sealed record DeleteExpenseRecordCommand(Guid Id, Guid ApplicationId) : IRequest, IBusinessAction
{
    public string ActionName => "Expense Record Deletion";
}
