using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.UsageBilling.Models;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.UsageBilling.Commands;

/// <summary>
/// Meters one billable clinic operation. Dispatched as an internal sub-command by the OPD create, IPD
/// create and IPD discharge handlers <b>before</b> their own <c>SaveChangesAsync</c>, so the
/// operation, its usage row and its wallet debit commit as a single transaction — a refusal therefore
/// commits nothing at all.
///
/// Audited on purpose, even though it is a sub-command: it moves money. <c>plan/action-audit-logging</c>
/// records that sub-commands like <c>EnsureOpdPaymentCommand</c> stay silent unless deliberately
/// added; this one is deliberately added, so a metered charge leaves a durable trail even if the
/// parent command's own row ages out.
/// </summary>
public sealed record ChargeUsageCommand(
    Guid ApplicationId,
    BillingItem BillingItem,
    Guid SourceEntityId,
    int Quantity,
    Guid? UserId,
    DateTime? OccurredAt)
    : IRequest<UsageChargeDto>, IBusinessAction, IActionLogContext
{
    public string ActionName => "Usage Charged";

    /// <summary>
    /// Identifiers only. The amount, the live mode and the resulting wallet transaction are known
    /// only <i>after</i> the handler runs, and <c>ActionLoggingBehavior</c> reads this property once,
    /// <b>before</b> the handler — so they cannot be reported here. The amount lives on the
    /// <c>ClinicUsageRecord</c> row and, when charged, on the wallet ledger, both keyed by
    /// <see cref="SourceEntityId"/>.
    /// </summary>
    public string ActionLogContext =>
        $"applicationId={ApplicationId}; billingItem={BillingItem}; quantity={Quantity}; sourceEntityId={SourceEntityId}";
}
