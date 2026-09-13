using MediatR;
using TenantCore.Application.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.AmountHandovers.Commands;

public sealed record CreateAmountHandoverCommand(CreateAmountHandoverRequest Request, Guid HandedOverByUserId, Guid ApplicationId) : IRequest<Guid>, IBusinessAction
{
    public string ActionName => "Amount Handover - Create";
}
