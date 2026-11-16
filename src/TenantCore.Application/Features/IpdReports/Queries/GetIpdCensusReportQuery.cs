using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdReports.Queries;

public sealed record GetIpdCensusReportQuery(Guid ApplicationId) : IRequest<IpdCensusReportDto>;
