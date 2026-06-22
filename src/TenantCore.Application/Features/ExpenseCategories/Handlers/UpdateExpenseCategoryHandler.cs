using MediatR;
using TenantCore.Application.Features.ExpenseCategories.Commands;
using TenantCore.Application.Features.ExpenseCategories.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ExpenseCategories.Handlers;

public sealed class UpdateExpenseCategoryHandler(IExpenseCategoryRepository repository) : IRequestHandler<UpdateExpenseCategoryCommand, ExpenseCategoryDto>
{
    public async Task<ExpenseCategoryDto> Handle(UpdateExpenseCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (category is null || category.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(ExpenseCategory), request.Id);

        category.Update(request.Request.Name, request.Request.Description, request.Request.IsActive);
        repository.Update(category);
        await repository.SaveChangesAsync(cancellationToken);
        return ExpenseCategoryTranslator.ToDto(category);
    }
}
