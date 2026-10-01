using TenantCore.Domain.Entities;

namespace TenantCore.Application.Features.Subscriptions.Services;

/// <summary>One plan as a particular clinic sees it — the list price plus whatever that clinic actually pays.</summary>
public sealed record ClinicPlanOption(
    SubscriptionPlan Plan,
    decimal EffectivePrice,
    bool IsSpecialOffer,
    DateTime? OfferValidUntil);

/// <summary>
/// The single place that answers "which plans may this clinic see, and at what price".
///
/// Visibility: an active plan is visible when it is public (and the clinic is not restricted to
/// its own offers) or when the clinic has a live ClinicPlanOffer for it.
///
/// Price: the live offer's price when it sets one, otherwise the plan's list price.
///
/// Used by the clinic's plan list, its renewal link, and the admin's price comparison.
/// </summary>
public interface IClinicPlanCatalog
{
    /// <summary>Visible, active plans for one clinic, in display order.</summary>
    Task<IReadOnlyList<ClinicPlanOption>> GetPlansForClinicAsync(Guid applicationId, CancellationToken ct = default);

    /// <summary>Whether the clinic may pick this plan itself. Admin assignment deliberately ignores this.</summary>
    Task<bool> IsVisibleAsync(Guid applicationId, Guid subscriptionPlanId, CancellationToken ct = default);

    /// <summary>
    /// What this clinic pays for the plan — its live offer price, else the list price. Answered
    /// regardless of visibility, because an admin may assign any active plan to any clinic.
    /// </summary>
    Task<decimal> GetEffectivePriceAsync(Guid applicationId, SubscriptionPlan plan, CancellationToken ct = default);
}
