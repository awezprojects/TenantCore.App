using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdPayments.Commands;

public sealed record ProcessOpdRefundCommand(
    ProcessOpdRefundRequest Request,
    Guid RefundedByUserId,
    Guid ApplicationId) : IRequest<OpdPaymentDto>;
