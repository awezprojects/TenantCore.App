using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.DoctorFeeConfigs.Queries;

public sealed record GetDoctorFeeConfigByIdQuery(Guid Id, Guid ApplicationId) : IRequest<DoctorFeeConfigDto>;
