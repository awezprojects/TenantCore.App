using MediatR;
using TenantCore.Application.Features.ExpenseCategories.Queries;
using TenantCore.Application.Features.ExpenseCategories.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ExpenseCategories.Handlers;

public sealed class GetExpenseCategoryByIdHandler(IExpenseCategoryRepository repository) : IRequestHandler<GetExpenseCategoryByIdQuery, ExpenseCategoryDto>
{
    public async Task<ExpenseCategoryDto> Handle(GetExpenseCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (category is null || category.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(ExpenseCategory), request.Id);

        return ExpenseCategoryTranslator.ToDto(category);
    }
}
