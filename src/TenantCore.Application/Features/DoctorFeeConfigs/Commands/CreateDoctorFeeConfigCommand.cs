using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.DoctorFeeConfigs.Commands;

public sealed record CreateDoctorFeeConfigCommand(CreateDoctorFeeConfigRequest Request, Guid ApplicationId) : IRequest<Guid>;
