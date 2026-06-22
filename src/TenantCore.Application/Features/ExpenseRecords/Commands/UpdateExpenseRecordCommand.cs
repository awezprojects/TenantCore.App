using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ExpenseRecords.Commands;

public sealed record UpdateExpenseRecordCommand(Guid Id, UpdateExpenseRecordRequest Request, Guid ApplicationId) : IRequest<ExpenseRecordDto>;
