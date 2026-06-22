using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ExpenseRecords.Queries;

public sealed record GetExpenseRecordsQuery(
    Guid ApplicationId,
    DateTime? From = null,
    DateTime? To = null,
    int UtcOffsetMinutes = 0) : IRequest<IEnumerable<ExpenseRecordSummaryDto>>;
