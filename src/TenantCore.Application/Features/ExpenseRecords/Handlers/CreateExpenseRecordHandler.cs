using MediatR;
using TenantCore.Application.Features.ExpenseRecords.Commands;
using TenantCore.Application.Features.ExpenseRecords.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.ExpenseRecords.Handlers;

public sealed class CreateExpenseRecordHandler(
    IExpenseRecordRepository expenseRecordRepository,
    IExpenseCategoryRepository expenseCategoryRepository,
    ICounterSessionRepository counterSessionRepository) : IRequestHandler<CreateExpenseRecordCommand, Guid>
{
    public async Task<Guid> Handle(CreateExpenseRecordCommand request, CancellationToken cancellationToken)
    {
        var activeSession = await counterSessionRepository.GetActiveSessionAsync(request.ApplicationId, cancellationToken);
        if (activeSession is null)
            throw new InvalidOperationException("No active counter session. Please open a counter session before recording expenses.");

        var category = await expenseCategoryRepository.GetByIdAsync(request.Request.ExpenseCategoryId, cancellationToken);
        if (category is null || category.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(ExpenseCategory), request.Request.ExpenseCategoryId);

        if (!category.IsActive)
            throw new InvalidOperationException("The selected expense category is inactive.");

        var record = ExpenseRecordTranslator.ToEntity(request, category.Name);
        await expenseRecordRepository.AddAsync(record, cancellationToken);
        await expenseRecordRepository.SaveChangesAsync(cancellationToken);
        return record.Id;
    }
}
