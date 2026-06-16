using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Obstetrics.Queries;

public sealed record GetUsgChartByPatientQuery(Guid PatientId, Guid ApplicationId) : IRequest<UsgChartDto>;
