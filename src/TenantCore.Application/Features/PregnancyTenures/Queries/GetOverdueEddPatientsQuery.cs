using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.PregnancyTenures.Queries;

public sealed record GetOverdueEddPatientsQuery(Guid ApplicationId) : IRequest<IEnumerable<PregnancyTenureSummaryDto>>;
