using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.Subscriptions.Services;

/// <inheritdoc />
public sealed class ClinicPlanCatalog(
    ISubscriptionPlanRepository planRepository,
    IClinicPlanOfferRepository offerRepository,
    IClinicAccountRepository accountRepository)
    : IClinicPlanCatalog
{
    public async Task<IReadOnlyList<ClinicPlanOption>> GetPlansForClinicAsync(Guid applicationId, CancellationToken ct = default)
    {
        var utcNow = DateTime.UtcNow;

        var plans = await planRepository.GetActivePlansAsync(ct);
        var offers = await offerRepository.GetLiveForClinicAsync(applicationId, utcNow, ct);
        var account = await accountRepository.GetByApplicationIdAsync(applicationId, ct);
        var restricted = account?.RestrictToOfferedPlans ?? false;

        var offersByPlan = offers
            .GroupBy(o => o.SubscriptionPlanId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(o => o.CreatedAt).First());

        var options = new List<ClinicPlanOption>();

        foreach (var plan in plans)
        {
            offersByPlan.TryGetValue(plan.Id, out var offer);

            // A restricted clinic sees only what it has been offered; everyone else also sees the
            // public catalogue. A non-public plan is always offer-only.
            var visible = offer is not null || (plan.IsPublic && !restricted);
            if (!visible)
                continue;

            options.Add(new ClinicPlanOption(
                plan,
                offer?.OfferPrice ?? plan.Price,
                IsSpecialOffer: offer?.OfferPrice is not null,
                OfferValidUntil: offer?.ValidUntil));
        }

        return options
            .OrderBy(o => o.Plan.DisplayOrder)
            .ThenBy(o => o.Plan.Name)
            .ToList();
    }

    public async Task<bool> IsVisibleAsync(Guid applicationId, Guid subscriptionPlanId, CancellationToken ct = default)
    {
        var plans = await GetPlansForClinicAsync(applicationId, ct);
        return plans.Any(o => o.Plan.Id == subscriptionPlanId);
    }

    public async Task<decimal> GetEffectivePriceAsync(Guid applicationId, SubscriptionPlan plan, CancellationToken ct = default)
    {
        var offer = await offerRepository.GetActiveForClinicAndPlanAsync(applicationId, plan.Id, ct);

        return offer is not null && offer.IsLive(DateTime.UtcNow) && offer.OfferPrice is { } price
            ? price
            : plan.Price;
    }
}
