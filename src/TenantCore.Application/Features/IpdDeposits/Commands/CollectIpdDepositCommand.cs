using MediatR;
using TenantCore.Application.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdDeposits.Commands;

public sealed record CollectIpdDepositCommand(
    Guid IpdRegistrationId,
    Guid ApplicationId,
    decimal Amount,
    Guid CollectedByUserId,
    string? Notes) : IRequest<IpdDepositDto>, IActionLogContext
{
    public string? ActionLogContext => $"ipdRegistrationId={IpdRegistrationId}; amount={Amount}";
}
