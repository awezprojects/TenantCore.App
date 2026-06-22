using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.FinanceReports.Queries;

public sealed record GetExpenseSummaryReportQuery(DateTime From, DateTime To, Guid ApplicationId, int UtcOffsetMinutes = 0) : IRequest<ExpenseSummaryReportDto>;
