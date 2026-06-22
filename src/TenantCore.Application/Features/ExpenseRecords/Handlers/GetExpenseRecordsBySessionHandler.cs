using MediatR;
using TenantCore.Application.Features.ExpenseRecords.Queries;
using TenantCore.Application.Features.ExpenseRecords.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ExpenseRecords.Handlers;

public sealed class GetExpenseRecordsBySessionHandler(IExpenseRecordRepository repository)
    : IRequestHandler<GetExpenseRecordsBySessionQuery, IEnumerable<ExpenseRecordSummaryDto>>
{
    public async Task<IEnumerable<ExpenseRecordSummaryDto>> Handle(GetExpenseRecordsBySessionQuery request, CancellationToken cancellationToken)
    {
        var records = await repository.GetBySessionIdAsync(request.SessionId, request.ApplicationId, cancellationToken);
        return records.Select(ExpenseRecordTranslator.ToSummaryDto);
    }
}
