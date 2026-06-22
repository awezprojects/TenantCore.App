using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.DoctorFeeConfigs.Queries;

public sealed record GetDoctorFeeConfigsQuery(Guid ApplicationId) : IRequest<IEnumerable<DoctorFeeConfigSummaryDto>>;
