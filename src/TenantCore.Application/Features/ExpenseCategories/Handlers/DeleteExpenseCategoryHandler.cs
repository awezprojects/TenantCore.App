using MediatR;
using TenantCore.Application.Features.ExpenseCategories.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.ExpenseCategories.Handlers;

public sealed class DeleteExpenseCategoryHandler(IExpenseCategoryRepository repository) : IRequestHandler<DeleteExpenseCategoryCommand>
{
    public async Task Handle(DeleteExpenseCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (category is null || category.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(ExpenseCategory), request.Id);

        repository.Delete(category);
        await repository.SaveChangesAsync(cancellationToken);
    }
}
