using MediatR;
using TenantCore.Application.Features.ExpenseRecords.Commands;
using TenantCore.Application.Features.ExpenseRecords.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ExpenseRecords.Handlers;

public sealed class PayExpenseRecordHandler(IExpenseRecordRepository repository) : IRequestHandler<PayExpenseRecordCommand, ExpenseRecordDto>
{
    public async Task<ExpenseRecordDto> Handle(PayExpenseRecordCommand request, CancellationToken cancellationToken)
    {
        var expense = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (expense is null || expense.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(ExpenseRecord), request.Id);

        expense.Pay(request.Request.PayAmount);
        repository.Update(expense);
        await repository.SaveChangesAsync(cancellationToken);
        return ExpenseRecordTranslator.ToDto(expense);
    }
}
