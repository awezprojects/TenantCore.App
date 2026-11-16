using TenantCore.Domain.Common;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

// Mirrors OpdPayment's role, extended with room charges and deposit netting. 1:1 with
// IpdRegistration, auto-created at admission when billing is enabled (same as OpdPayment).
//
// RoomChargesTotal/TotalAmount/FinalAmount/BalanceDue are only meaningful once FinalizedAt is
// set — before that, the live "as of now" numbers come from GetIpdProvisionalBillQuery (which
// calls IIpdBillCalculator directly and never persists), not from this entity. ParticularsTotal,
// TotalDeposited and Discount ARE kept live here, incrementally, by their own commands, because
// they are simple ledger sums rather than time-derived room charges.
public class IpdPayment : AuditableEntity
{
    public Guid ApplicationId { get; private set; }
    public Guid IpdRegistrationId { get; private set; }
    public decimal InitialFee { get; private set; }
    public decimal RoomChargesTotal { get; private set; }
    public decimal ParticularsTotal { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal Discount { get; private set; }
    public Guid? DiscountedByUserId { get; private set; }
    public DateTime? DiscountedAt { get; private set; }
    public decimal FinalAmount { get; private set; }
    public decimal TotalDeposited { get; private set; }
    public decimal BalanceDue { get; private set; }
    public DateTime? FinalizedAt { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public DateTime? SettledAt { get; private set; }
    public Guid? SettledByUserId { get; private set; }
    public Guid? CounterSessionId { get; private set; }
    public decimal RefundDue { get; private set; }
    public RefundStatus RefundStatus { get; private set; }
    public DateTime? RefundedAt { get; private set; }
    public Guid? RefundedByUserId { get; private set; }

    private IpdPayment() { }

    public static IpdPayment Create(Guid applicationId, Guid ipdRegistrationId, decimal initialFee) => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = applicationId,
        IpdRegistrationId = ipdRegistrationId,
        InitialFee = initialFee,
        TotalAmount = initialFee,
        FinalAmount = initialFee,
        PaymentStatus = PaymentStatus.Pending,
        RefundStatus = RefundStatus.None,
        CreatedAt = DateTime.UtcNow
    };

    public void IncreaseParticularsTotal(decimal amount)
    {
        if (FinalizedAt is not null)
            throw new InvalidOperationException("Cannot add a service item after the bill has been finalized.");
        ParticularsTotal += amount;
        SetUpdatedAt();
    }

    public void DecreaseParticularsTotal(decimal amount)
    {
        if (FinalizedAt is not null)
            throw new InvalidOperationException("Cannot remove a service item after the bill has been finalized.");
        ParticularsTotal -= amount;
        SetUpdatedAt();
    }

    public void IncreaseDeposited(decimal amount)
    {
        TotalDeposited += amount;
        SetUpdatedAt();
    }

    public void ApplyDiscount(decimal discount, Guid discountedByUserId)
    {
        if (FinalizedAt is not null)
            throw new InvalidOperationException("Cannot change the discount after the bill has been finalized.");
        Discount = discount;
        DiscountedByUserId = discountedByUserId;
        DiscountedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    // Locks the bill at discharge (as-of DischargeDate). Collecting the resulting balance or
    // processing a refund is a separate, later action — this only determines the numbers.
    public void FinalizeAtDischarge(decimal roomChargesTotal, DateTime dischargeDate)
    {
        if (FinalizedAt is not null)
            throw new InvalidOperationException("This bill has already been finalized.");

        RoomChargesTotal = roomChargesTotal;
        TotalAmount = InitialFee + RoomChargesTotal + ParticularsTotal;
        FinalAmount = TotalAmount - Discount;
        BalanceDue = FinalAmount - TotalDeposited;
        FinalizedAt = dischargeDate;

        if (BalanceDue <= 0)
        {
            PaymentStatus = PaymentStatus.Received;
            if (BalanceDue < 0)
            {
                RefundDue = -BalanceDue;
                RefundStatus = RefundStatus.PendingRefund;
            }
        }

        SetUpdatedAt();
    }

    // Locks the bill at ₹0 when an admission is cancelled (DEF-IPD-19): a cancelled stay is a
    // mistake being undone, not a stay that happened — no room charge and no initial fee, unlike
    // a normal discharge's FinalizeAtDischarge. The caller (CancelIpdRegistrationHandler) is
    // responsible for blocking the cancel while ParticularsTotal > 0, so this method never has to
    // reconcile service items into a ₹0 bill. Any deposit already collected becomes a full refund.
    public void FinalizeAtCancellation(DateTime cancelledAt)
    {
        if (FinalizedAt is not null)
            throw new InvalidOperationException("This bill has already been finalized.");

        RoomChargesTotal = 0m;
        TotalAmount = 0m;
        FinalAmount = 0m;
        BalanceDue = -TotalDeposited;
        FinalizedAt = cancelledAt;
        PaymentStatus = PaymentStatus.Received;

        if (TotalDeposited > 0m)
        {
            RefundDue = TotalDeposited;
            RefundStatus = RefundStatus.PendingRefund;
        }

        SetUpdatedAt();
    }

    public void CollectBalance(Guid receivedByUserId, Guid? counterSessionId)
    {
        if (FinalizedAt is null)
            throw new InvalidOperationException("The bill has not been finalized yet.");
        if (BalanceDue <= 0)
            throw new InvalidOperationException("No balance is due.");
        if (PaymentStatus == PaymentStatus.Received)
            throw new InvalidOperationException("The balance has already been collected.");

        PaymentStatus = PaymentStatus.Received;
        SettledAt = DateTime.UtcNow;
        SettledByUserId = receivedByUserId;
        CounterSessionId = counterSessionId;
        SetUpdatedAt();
    }

    public void ProcessRefund(Guid refundedByUserId)
    {
        if (RefundStatus != RefundStatus.PendingRefund)
            throw new InvalidOperationException("No refund is pending for this payment.");

        RefundStatus = RefundStatus.Refunded;
        RefundedAt = DateTime.UtcNow;
        RefundedByUserId = refundedByUserId;
        SetUpdatedAt();
    }
}
