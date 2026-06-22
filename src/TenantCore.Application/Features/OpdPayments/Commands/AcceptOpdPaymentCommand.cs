using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdPayments.Commands;

public sealed record AcceptOpdPaymentCommand(AcceptOpdPaymentRequest Request, Guid ReceivedByUserId, Guid ApplicationId) : IRequest<OpdPaymentDto>;
