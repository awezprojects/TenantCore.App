using MediatR;

namespace TenantCore.Application.Features.DoctorFeeConfigs.Commands;

public sealed record DeleteDoctorFeeConfigCommand(Guid Id, Guid ApplicationId) : IRequest;
