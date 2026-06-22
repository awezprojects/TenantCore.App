using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ExpenseRecords.Queries;

public sealed record GetExpenseRecordByIdQuery(Guid Id, Guid ApplicationId) : IRequest<ExpenseRecordDto>;
