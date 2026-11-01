using MediatR;
using TenantCore.Application.Features.Subscriptions.Queries;
using TenantCore.Application.Features.Subscriptions.Services;
using TenantCore.Application.Features.Subscriptions.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos.Subscriptions;

namespace TenantCore.Application.Features.Subscriptions.Handlers;

public sealed class GetSubscriptionPlansHandler(
    IClinicPlanCatalog planCatalog,
    IClinicSubscriptionRepository subscriptionRepository)
    : IRequestHandler<GetSubscriptionPlansQuery, IEnumerable<SubscriptionPlanDto>>
{
    public async Task<IEnumerable<SubscriptionPlanDto>> Handle(GetSubscriptionPlansQuery request, CancellationToken cancellationToken)
    {
        // Which plans this clinic may see, and at what price — public catalogue plus its own
        // offers, or offers only when an admin restricted it. See IClinicPlanCatalog.
        var options = await planCatalog.GetPlansForClinicAsync(request.ApplicationId, cancellationToken);

        // Any subscription history at all — not just a prior trial — rules the Trial card out,
        // since a clinic created through onboarding always already has one (its trial or paid
        // plan) by the time it can reach this page. See SubscribeToPlanHandler.
        var hasAnyHistory = await subscriptionRepository.HasAnySubscriptionHistoryAsync(request.ApplicationId, cancellationToken);

        return options.Select(o => SubscriptionTranslator.ToPlanDto(
            o.Plan,
            alreadyUsed: o.Plan.IsTrial && hasAnyHistory,
            effectivePrice: o.EffectivePrice,
            isSpecialOffer: o.IsSpecialOffer,
            offerValidUntil: o.OfferValidUntil));
    }
}
