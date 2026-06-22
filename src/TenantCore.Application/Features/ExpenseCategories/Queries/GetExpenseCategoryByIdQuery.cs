using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ExpenseCategories.Queries;

public sealed record GetExpenseCategoryByIdQuery(Guid Id, Guid ApplicationId) : IRequest<ExpenseCategoryDto>;
