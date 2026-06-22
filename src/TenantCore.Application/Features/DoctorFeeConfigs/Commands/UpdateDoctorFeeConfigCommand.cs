using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.DoctorFeeConfigs.Commands;

public sealed record UpdateDoctorFeeConfigCommand(Guid Id, UpdateDoctorFeeConfigRequest Request, Guid ApplicationId) : IRequest<DoctorFeeConfigDto>;
