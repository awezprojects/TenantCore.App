using MediatR;
using TenantCore.Application.Common;

namespace TenantCore.Application.Features.ExpenseCategories.Commands;

public sealed record DeleteExpenseCategoryCommand(Guid Id, Guid ApplicationId) : IRequest, IBusinessAction
{
    public string ActionName => "Expense Category Deletion";
}
