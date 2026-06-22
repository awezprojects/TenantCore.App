using MediatR;
using TenantCore.Application.Features.ExpenseRecords.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.ExpenseRecords.Handlers;

public sealed class DeleteExpenseRecordHandler(IExpenseRecordRepository repository) : IRequestHandler<DeleteExpenseRecordCommand>
{
    public async Task Handle(DeleteExpenseRecordCommand request, CancellationToken cancellationToken)
    {
        var record = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (record is null || record.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(ExpenseRecord), request.Id);

        repository.Delete(record);
        await repository.SaveChangesAsync(cancellationToken);
    }
}
