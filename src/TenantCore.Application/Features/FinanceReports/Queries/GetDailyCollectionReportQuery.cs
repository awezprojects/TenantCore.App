using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.FinanceReports.Queries;

public sealed record GetDailyCollectionReportQuery(DateTime Date, Guid ApplicationId, int UtcOffsetMinutes = 0) : IRequest<DailyCollectionReportDto>;
