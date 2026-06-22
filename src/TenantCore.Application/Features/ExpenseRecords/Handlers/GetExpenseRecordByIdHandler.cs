using MediatR;
using TenantCore.Application.Features.ExpenseRecords.Queries;
using TenantCore.Application.Features.ExpenseRecords.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ExpenseRecords.Handlers;

public sealed class GetExpenseRecordByIdHandler(IExpenseRecordRepository repository) : IRequestHandler<GetExpenseRecordByIdQuery, ExpenseRecordDto>
{
    public async Task<ExpenseRecordDto> Handle(GetExpenseRecordByIdQuery request, CancellationToken cancellationToken)
    {
        var record = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (record is null || record.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(ExpenseRecord), request.Id);

        return ExpenseRecordTranslator.ToDto(record);
    }
}
