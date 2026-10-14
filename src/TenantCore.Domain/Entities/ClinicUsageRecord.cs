using TenantCore.Domain.Common;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

/// <summary>
/// The metered-usage ledger. Tenant-scoped, append-only, one row per billable operation for
/// <b>every</b> clinic regardless of billing mode — so a subscription clinic's usage is answerable
/// today and convertible into pay-as-you-go pricing tomorrow.
///
/// The unique index on (ApplicationId, BillingItem, SourceEntityId) is the idempotency guarantee:
/// a retried request, a duplicated workflow task or a replayed call can never produce a second
/// charge for the same operation. This is what makes the whole design safe to retry.
/// </summary>
public class ClinicUsageRecord : AuditableEntity
{
    public Guid ApplicationId { get; private set; }

    public BillingItem BillingItem { get; private set; }

    /// <summary>The <c>OpdRegistration.Id</c> / <c>IpdRegistration.Id</c> that caused this row.</summary>
    public Guid SourceEntityId { get; private set; }

    /// <summary>Kept so a future item can be per-day or per-session without a migration.</summary>
    public int Quantity { get; private set; }

    /// <summary>decimal(18,4) — a <b>snapshot</b> of the live rate at charge time, 0 when unpriced.</summary>
    public decimal UnitRate { get; private set; }

    /// <summary>decimal(18,2) — <c>UnitRate × Quantity</c> when charged, otherwise 0. What the clinic actually paid.</summary>
    public decimal Amount { get; private set; }

    /// <summary>Which mode applied — Subscription (charged 0) or PayAsYouGo.</summary>
    public BillingMode PayerMode { get; private set; }

    /// <summary>True when nothing was charged: a subscription clinic, or an unpriced operation.</summary>
    public bool IsFree { get; private set; }

    /// <summary>The paired debit row. Null whenever <see cref="IsFree"/>.</summary>
    public Guid? WalletTransactionId { get; private set; }

    /// <summary>UTC — the operation's timestamp, not the row's insert time.</summary>
    public DateTime ChargedAt { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    private ClinicUsageRecord() { }

    /// <summary>
    /// Records one metered operation. <paramref name="unitRate"/> is the rate in force at charge
    /// time (0 when the item is unpriced). A subscription clinic — or an unpriced item — produces a
    /// free row, which is still written.
    /// </summary>
    public static ClinicUsageRecord Create(
        Guid applicationId, BillingItem billingItem, Guid sourceEntityId, decimal unitRate,
        BillingMode payerMode, DateTime chargedAt, int quantity = 1, Guid? createdByUserId = null,
        bool forceFree = false)
    {
        if (quantity < 1)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Usage quantity must be at least 1.");

        // A subscription clinic is metered but never charged; an unpriced item is free, not blocked.
        // forceFree covers the UsageBilling:Enabled kill switch, which still records the rate that
        // applied so switching metering back on needs no backfill and analytics keep working.
        var isFree = forceFree || payerMode == BillingMode.Subscription || unitRate == 0m;

        return new ClinicUsageRecord
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            BillingItem = billingItem,
            SourceEntityId = sourceEntityId,
            Quantity = quantity,
            UnitRate = unitRate,
            Amount = isFree ? 0m : Math.Round(unitRate * quantity, 2, MidpointRounding.AwayFromZero),
            PayerMode = payerMode,
            IsFree = isFree,
            ChargedAt = chargedAt,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Links the wallet debit that paid for this row. Only meaningful when something was charged —
    /// a free row must never point at a ledger movement that does not exist.
    /// </summary>
    public void AttachWalletTransaction(Guid walletTransactionId)
    {
        if (IsFree)
            throw new InvalidOperationException("A free usage record cannot be linked to a wallet transaction.");

        WalletTransactionId = walletTransactionId;
        SetUpdatedAt();
    }
}
