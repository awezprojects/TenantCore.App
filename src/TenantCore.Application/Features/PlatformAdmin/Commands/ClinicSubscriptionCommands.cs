using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.PlatformAdmin.Models;

namespace TenantCore.Application.Features.PlatformAdmin.Commands;

/// <summary>
/// Internal — activates a plan for free. The term is queued after the clinic's existing coverage
/// exactly like a paid one, so it never overlaps or leaves a gap. Returns the new subscription id.
/// </summary>
public sealed record GrantClinicSubscriptionCommand(
    Guid ApplicationId,
    Guid SubscriptionPlanId,
    string Reason,
    ClinicContact Contact,
    Guid AdminUserId,
    string AdminEmail)
    : IRequest<Guid>, IBusinessAction, IActionLogContext
{
    public string ActionName => "Subscription Granted by Admin";

    // The grant reason and the contact snapshot are deliberately absent — identifiers only.
    public string ActionLogContext => $"applicationId={ApplicationId}; planId={SubscriptionPlanId}; adminUserId={AdminUserId}";
}

/// <summary>
/// Internal — creates a Razorpay payment link for an existing clinic and emails it. Supersedes any
/// link the clinic already has open. Returns the new payment id.
/// </summary>
public sealed record AssignPlanPaymentLinkCommand(
    Guid ApplicationId,
    Guid SubscriptionPlanId,
    decimal Amount,
    string? AmountReason,
    ClinicContact Contact,
    Guid AdminUserId,
    string AdminEmail)
    : IRequest<Guid>, IBusinessAction, IActionLogContext
{
    public string ActionName => "Plan Payment Link Sent by Admin";

    public string ActionLogContext => $"applicationId={ApplicationId}; planId={SubscriptionPlanId}; adminUserId={AdminUserId}";
}

/// <summary>Internal — cancels a term that has not started, then re-chains any later ones so no gap opens.</summary>
public sealed record CancelUpcomingSubscriptionCommand(
    Guid ApplicationId,
    Guid SubscriptionId,
    string Reason,
    Guid AdminUserId,
    string AdminEmail)
    : IRequest, IBusinessAction, IActionLogContext
{
    public string ActionName => "Upcoming Subscription Cancelled";

    public string ActionLogContext => $"applicationId={ApplicationId}; subscriptionId={SubscriptionId}; adminUserId={AdminUserId}";
}

/// <summary>Internal — cancels an open clinic payment link (renewal or admin-assigned) with no replacement.</summary>
public sealed record CancelClinicPaymentLinkCommand(
    Guid ApplicationId,
    Guid PaymentId,
    Guid AdminUserId,
    string AdminEmail)
    : IRequest, IBusinessAction, IActionLogContext
{
    public string ActionName => "Clinic Payment Link Cancelled";

    public string ActionLogContext => $"applicationId={ApplicationId}; paymentId={PaymentId}; adminUserId={AdminUserId}";
}
