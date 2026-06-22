using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ExpenseRecords.Commands;

public sealed record PayExpenseRecordCommand(Guid Id, PayExpenseRecordRequest Request, Guid ApplicationId) : IRequest<ExpenseRecordDto>;
