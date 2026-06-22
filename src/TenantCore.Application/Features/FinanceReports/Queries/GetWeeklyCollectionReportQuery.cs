using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.FinanceReports.Queries;

public sealed record GetWeeklyCollectionReportQuery(DateTime WeekStartDate, Guid ApplicationId) : IRequest<WeeklyCollectionReportDto>;
