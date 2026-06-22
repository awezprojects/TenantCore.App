using MediatR;
using TenantCore.Application.Features.ExpenseCategories.Queries;
using TenantCore.Application.Features.ExpenseCategories.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ExpenseCategories.Handlers;

public sealed class GetExpenseCategoriesHandler(IExpenseCategoryRepository repository) : IRequestHandler<GetExpenseCategoriesQuery, IEnumerable<ExpenseCategorySummaryDto>>
{
    public async Task<IEnumerable<ExpenseCategorySummaryDto>> Handle(GetExpenseCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await repository.GetAllAsync(cancellationToken);
        return categories
            .Where(c => c.ApplicationId == request.ApplicationId)
            .Select(c => ExpenseCategoryTranslator.ToSummaryDto(c));
    }
}
