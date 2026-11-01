using MediatR;
using TenantCore.Application.Common;
using TenantCore.Shared.Dtos.Subscriptions;

namespace TenantCore.Application.Features.Subscriptions.Commands;

public sealed record CreateRenewalPaymentLinkCommand(Guid ApplicationId, Guid SubscriptionPlanId, Guid ActingUserId)
    : IRequest<SubscriptionPaymentDto>, IBusinessAction
{
    public string ActionName => "Renewal Payment Link Requested";
}
