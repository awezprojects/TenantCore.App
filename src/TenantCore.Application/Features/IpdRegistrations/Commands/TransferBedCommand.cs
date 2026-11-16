using MediatR;
using TenantCore.Application.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdRegistrations.Commands;

public sealed record TransferBedCommand(
    Guid Id,
    Guid ApplicationId,
    Guid NewBedId,
    Guid TransferredByUserId,
    string? Notes,
    decimal? ManualChargeAmount = null,
    string? ManualChargeReason = null) : IRequest<IpdRegistrationDto>, IActionLogContext
{
    public string? ActionLogContext => $"ipdRegistrationId={Id}; newBedId={NewBedId}";
}
