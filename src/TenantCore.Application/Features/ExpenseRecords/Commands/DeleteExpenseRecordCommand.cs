using MediatR;

namespace TenantCore.Application.Features.ExpenseRecords.Commands;

public sealed record DeleteExpenseRecordCommand(Guid Id, Guid ApplicationId) : IRequest;
