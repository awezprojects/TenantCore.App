using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdPayments.Commands;

// Collects the entire bill (visit fee + all services - discount) in a single transaction.
// Only valid when OpdPayment.Discount > 0. Also marks all pending OpdParticulars as received.
public sealed record AcceptOpdPaymentFullCommand(
    AcceptOpdPaymentFullRequest Request,
    Guid ReceivedByUserId,
    Guid ApplicationId) : IRequest<OpdPaymentDto>;
