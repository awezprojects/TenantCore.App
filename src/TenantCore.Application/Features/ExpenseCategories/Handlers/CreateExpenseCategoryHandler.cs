using MediatR;
using TenantCore.Application.Features.ExpenseCategories.Commands;
using TenantCore.Application.Features.ExpenseCategories.Translators;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.ExpenseCategories.Handlers;

public sealed class CreateExpenseCategoryHandler(IExpenseCategoryRepository repository) : IRequestHandler<CreateExpenseCategoryCommand, Guid>
{
    public async Task<Guid> Handle(CreateExpenseCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = ExpenseCategoryTranslator.ToEntity(request);
        await repository.AddAsync(category, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return category.Id;
    }
}
