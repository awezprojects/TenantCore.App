using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdPayments.Commands;

public sealed record ApplyOpdDiscountCommand(ApplyOpdDiscountRequest Request, Guid ApplicationId) : IRequest<OpdPaymentDto>;
