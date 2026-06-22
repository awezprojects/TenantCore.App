using MediatR;

namespace TenantCore.Application.Features.ExpenseCategories.Commands;

public sealed record DeleteExpenseCategoryCommand(Guid Id, Guid ApplicationId) : IRequest;
