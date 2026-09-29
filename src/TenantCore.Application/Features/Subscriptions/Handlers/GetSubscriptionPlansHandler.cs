using MediatR;
using TenantCore.Application.Features.Subscriptions.Queries;
using TenantCore.Application.Features.Subscriptions.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos.Subscriptions;

namespace TenantCore.Application.Features.Subscriptions.Handlers;

public sealed class GetSubscriptionPlansHandler(
    ISubscriptionPlanRepository planRepository,
    IClinicSubscriptionRepository subscriptionRepository)
    : IRequestHandler<GetSubscriptionPlansQuery, IEnumerable<SubscriptionPlanDto>>
{
    public async Task<IEnumerable<SubscriptionPlanDto>> Handle(GetSubscriptionPlansQuery request, CancellationToken cancellationToken)
    {
        var plans = await planRepository.GetActivePlansAsync(cancellationToken);

        // Any subscription history at all — not just a prior trial — rules the Trial card out,
        // since a clinic created through onboarding always already has one (its trial or paid
        // plan) by the time it can reach this page. See SubscribeToPlanHandler.
        var hasAnyHistory = await subscriptionRepository.HasAnySubscriptionHistoryAsync(request.ApplicationId, cancellationToken);

        return plans.Select(p => SubscriptionTranslator.ToPlanDto(p, alreadyUsed: p.IsTrial && hasAnyHistory));
    }
}
