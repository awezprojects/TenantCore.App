using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.DoctorFeeConfigs.Queries;

public sealed record GetDoctorFeeConfigByDoctorIdQuery(Guid DoctorProfileId, Guid ApplicationId) : IRequest<DoctorFeeConfigDto?>;
