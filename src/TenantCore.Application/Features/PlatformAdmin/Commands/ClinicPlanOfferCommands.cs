using MediatR;
using TenantCore.Application.Common;

namespace TenantCore.Application.Features.PlatformAdmin.Commands;

/// <summary>Internal — decides whether the clinic sees the public catalogue or only its own offers.</summary>
public sealed record SetClinicPlanVisibilityCommand(
    Guid ApplicationId, bool RestrictToOfferedPlans, Guid AdminUserId, string AdminEmail)
    : IRequest, IBusinessAction, IActionLogContext
{
    public string ActionName => "Clinic Plan Visibility Changed";

    public string ActionLogContext =>
        $"applicationId={ApplicationId}; restrict={RestrictToOfferedPlans}; adminUserId={AdminUserId}";
}

/// <summary>
/// Internal — gives one clinic access to one plan, optionally at a price only it gets.
/// Returns the new offer id.
/// </summary>
public sealed record CreateClinicPlanOfferCommand(
    Guid ApplicationId,
    Guid SubscriptionPlanId,
    decimal? OfferPrice,
    DateTime? ValidUntil,
    string? Note,
    Guid AdminUserId,
    string AdminEmail)
    : IRequest<Guid>, IBusinessAction, IActionLogContext
{
    public string ActionName => "Clinic Plan Offer Created";

    public string ActionLogContext =>
        $"applicationId={ApplicationId}; planId={SubscriptionPlanId}; adminUserId={AdminUserId}";
}

/// <summary>Internal — withdraws a live offer. The row is kept for history.</summary>
public sealed record WithdrawClinicPlanOfferCommand(
    Guid ApplicationId, Guid OfferId, Guid AdminUserId, string AdminEmail)
    : IRequest, IBusinessAction, IActionLogContext
{
    public string ActionName => "Clinic Plan Offer Withdrawn";

    public string ActionLogContext => $"applicationId={ApplicationId}; offerId={OfferId}; adminUserId={AdminUserId}";
}
