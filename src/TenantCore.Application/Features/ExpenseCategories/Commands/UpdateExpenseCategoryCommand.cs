using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ExpenseCategories.Commands;

public sealed record UpdateExpenseCategoryCommand(Guid Id, UpdateExpenseCategoryRequest Request, Guid ApplicationId) : IRequest<ExpenseCategoryDto>;
