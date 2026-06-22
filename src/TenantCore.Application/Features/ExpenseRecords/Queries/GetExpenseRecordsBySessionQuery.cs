using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ExpenseRecords.Queries;

public sealed record GetExpenseRecordsBySessionQuery(Guid SessionId, Guid ApplicationId) : IRequest<IEnumerable<ExpenseRecordSummaryDto>>;
