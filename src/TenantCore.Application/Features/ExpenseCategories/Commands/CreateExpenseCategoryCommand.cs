using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ExpenseCategories.Commands;

public sealed record CreateExpenseCategoryCommand(CreateExpenseCategoryRequest Request, Guid ApplicationId) : IRequest<Guid>;
