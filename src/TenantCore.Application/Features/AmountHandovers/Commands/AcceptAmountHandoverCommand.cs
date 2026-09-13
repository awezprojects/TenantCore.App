using MediatR;
using TenantCore.Application.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.AmountHandovers.Commands;

public sealed record AcceptAmountHandoverCommand(Guid Id, Guid ApplicationId) : IRequest<AmountHandoverDto>, IBusinessAction
{
    public string ActionName => "Amount Handover - Accept";
}
