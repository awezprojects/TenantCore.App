using MediatR;
using TenantCore.Application.Features.ExpenseRecords.Queries;
using TenantCore.Application.Features.ExpenseRecords.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ExpenseRecords.Handlers;

public sealed class GetExpenseRecordsHandler(IExpenseRecordRepository repository) : IRequestHandler<GetExpenseRecordsQuery, IEnumerable<ExpenseRecordSummaryDto>>
{
    public async Task<IEnumerable<ExpenseRecordSummaryDto>> Handle(GetExpenseRecordsQuery request, CancellationToken cancellationToken)
    {
        var offsetMinutes = request.UtcOffsetMinutes;
        DateTime? fromUtc = request.From.HasValue
            ? request.From.Value.Date.AddMinutes(-offsetMinutes)
            : null;
        DateTime? toUtc = request.To.HasValue
            ? request.To.Value.Date.AddDays(1).AddMinutes(-offsetMinutes)
            : null;

        var records = await repository.GetByDateRangeAsync(fromUtc, toUtc, request.ApplicationId, cancellationToken);
        return records.Select(ExpenseRecordTranslator.ToSummaryDto);
    }
}
