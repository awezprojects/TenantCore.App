using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ClinicSettings.Commands;

public sealed record UpdateClinicFeeConfigCommand(
    Guid ApplicationId,
    decimal OpdFee) : IRequest<ClinicFeeConfigDto>;
