using MediatR;
using TenantCore.Application.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdPayments.Commands;

public sealed record ApplyOpdDiscountCommand(ApplyOpdDiscountRequest Request, Guid ApplicationId) : IRequest<OpdPaymentDto>, IBusinessAction
{
    public string ActionName => "OPD Discount";
}
