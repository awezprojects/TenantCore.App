using MediatR;
using TenantCore.Application.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdPayments.Commands;

public sealed record ApplyIpdDiscountCommand(
    Guid IpdRegistrationId,
    Guid ApplicationId,
    decimal Discount,
    Guid DiscountedByUserId) : IRequest<IpdProvisionalBillDto>, IActionLogContext
{
    public string? ActionLogContext => $"ipdRegistrationId={IpdRegistrationId}; discount={Discount}";
}
