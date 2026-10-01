using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.PlatformAdmin.Models;

namespace TenantCore.Application.Features.PlatformAdmin.Commands;

/// <summary>Internal — adds an admin-defined package to the global catalogue (Code = Custom). Returns the plan id.</summary>
public sealed record CreateSubscriptionPlanCommand(
    PlanDetails Details, Guid AdminUserId, string AdminEmail)
    : IRequest<Guid>, IBusinessAction, IActionLogContext
{
    public string ActionName => "Subscription Plan Created";

    public string ActionLogContext => $"adminUserId={AdminUserId}";
}

/// <summary>
/// Internal — edits a catalogue entry. Existing subscriptions and open links keep their snapshotted
/// price, name and duration, so this only affects future purchases.
/// </summary>
public sealed record UpdateSubscriptionPlanCommand(
    Guid PlanId, PlanDetails Details, Guid AdminUserId, string AdminEmail)
    : IRequest, IBusinessAction, IActionLogContext
{
    public string ActionName => "Subscription Plan Updated";

    public string ActionLogContext => $"planId={PlanId}; adminUserId={AdminUserId}";
}

/// <summary>Internal — activates or deactivates a plan. Deactivating never touches existing terms.</summary>
public sealed record SetSubscriptionPlanActiveCommand(
    Guid PlanId, bool IsActive, Guid AdminUserId, string AdminEmail)
    : IRequest, IBusinessAction, IActionLogContext
{
    public string ActionName => IsActive ? "Subscription Plan Activated" : "Subscription Plan Deactivated";

    public string ActionLogContext => $"planId={PlanId}; isActive={IsActive}; adminUserId={AdminUserId}";
}
