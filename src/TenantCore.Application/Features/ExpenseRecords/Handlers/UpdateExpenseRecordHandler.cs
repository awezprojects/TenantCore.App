using MediatR;
using TenantCore.Application.Features.ExpenseRecords.Commands;
using TenantCore.Application.Features.ExpenseRecords.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ExpenseRecords.Handlers;

public sealed class UpdateExpenseRecordHandler(IExpenseRecordRepository repository) : IRequestHandler<UpdateExpenseRecordCommand, ExpenseRecordDto>
{
    public async Task<ExpenseRecordDto> Handle(UpdateExpenseRecordCommand request, CancellationToken cancellationToken)
    {
        var record = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (record is null || record.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(ExpenseRecord), request.Id);

        record.UpdateAmount(request.Request.Amount, request.Request.Notes);
        repository.Update(record);
        await repository.SaveChangesAsync(cancellationToken);
        return ExpenseRecordTranslator.ToDto(record);
    }
}
