using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.UsageBilling.Models;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.UsageBilling.Commands;

/// <summary>
/// Reverses one metered charge — the symmetric counterpart of <see cref="ChargeUsageCommand"/>.
/// Dispatched as an internal sub-command, before the parent handler's own <c>SaveChangesAsync</c>,
/// so the reversal travels in the same transaction as whatever caused it (e.g. a cancelled OPD
/// registration). A no-op (no usage row, a free row, or an already-reversed row) is silent — callers
/// never need to check whether a charge actually happened before asking to reverse it.
/// </summary>
public sealed record ReverseUsageChargeCommand(
    Guid ApplicationId,
    BillingItem BillingItem,
    Guid SourceEntityId,
    Guid? UserId,
    DateTime? OccurredAt)
    : IRequest<UsageChargeDto>, IBusinessAction, IActionLogContext
{
    public string ActionName => "Usage Charge Reversed";

    public string ActionLogContext =>
        $"applicationId={ApplicationId}; billingItem={BillingItem}; sourceEntityId={SourceEntityId}";
}
