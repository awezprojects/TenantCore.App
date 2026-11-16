using MediatR;
using TenantCore.Application.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdPayments.Commands;

public sealed record ProcessIpdRefundCommand(
    Guid IpdRegistrationId,
    Guid ApplicationId,
    Guid RefundedByUserId) : IRequest<IpdFinalBillDto>, IActionLogContext
{
    public string? ActionLogContext => $"ipdRegistrationId={IpdRegistrationId}";
}
