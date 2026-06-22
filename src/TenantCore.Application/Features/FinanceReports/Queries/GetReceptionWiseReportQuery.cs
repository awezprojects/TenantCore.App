using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.FinanceReports.Queries;

public sealed record GetReceptionWiseReportQuery(DateTime From, DateTime To, Guid ApplicationId) : IRequest<IEnumerable<ReceptionWiseReportDto>>;
