using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdReports.Queries;

public sealed record GetIpdDischargeSummaryReportQuery(Guid ApplicationId, DateTime FromUtc, DateTime ToUtc) : IRequest<IpdDischargeSummaryReportDto>;
