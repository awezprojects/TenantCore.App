using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.FinanceReports.Queries;

public sealed record GetMonthlyCollectionReportQuery(int Year, int Month, Guid ApplicationId) : IRequest<MonthlyCollectionReportDto>;
