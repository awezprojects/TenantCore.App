using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.FinanceReports.Queries;

public sealed record GetFinanceDashboardSummaryQuery(DateTime Date, Guid ApplicationId) : IRequest<FinanceDashboardSummaryDto>;
