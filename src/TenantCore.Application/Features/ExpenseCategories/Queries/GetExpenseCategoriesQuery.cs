using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ExpenseCategories.Queries;

public sealed record GetExpenseCategoriesQuery(Guid ApplicationId) : IRequest<IEnumerable<ExpenseCategorySummaryDto>>;
