using MediatR;
using TenantCore.Application.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdPayments.Commands;

public sealed record CollectIpdBalanceCommand(
    Guid IpdRegistrationId,
    Guid ApplicationId,
    Guid ReceivedByUserId) : IRequest<IpdFinalBillDto>, IActionLogContext
{
    public string? ActionLogContext => $"ipdRegistrationId={IpdRegistrationId}";
}
