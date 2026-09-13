using MediatR;
using TenantCore.Application.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdPayments.Commands;

public sealed record ProcessOpdRefundCommand(
    ProcessOpdRefundRequest Request,
    Guid RefundedByUserId,
    Guid ApplicationId) : IRequest<OpdPaymentDto>, IBusinessAction
{
    public string ActionName => "OPD Refund";
}
