using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ExpenseRecords.Commands;

public sealed record CreateExpenseRecordCommand(CreateExpenseRecordRequest Request, Guid RecordedByUserId, Guid ApplicationId) : IRequest<Guid>;
