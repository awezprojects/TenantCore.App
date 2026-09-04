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
        var hasUsedTrial = await subscriptionRepository.HasUsedTrialAsync(request.ApplicationId, cancellationToken);

        return plans.Select(p => SubscriptionTranslator.ToPlanDto(p, alreadyUsed: p.IsTrial && hasUsedTrial));
    }
}
