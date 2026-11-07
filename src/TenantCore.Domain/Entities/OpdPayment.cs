using TenantCore.Domain.Common;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

public class OpdPayment : AuditableEntity
{
    public Guid ApplicationId { get; private set; }
    public Guid OpdRegistrationId { get; private set; }
    public decimal VisitFee { get; private set; }
    public decimal ParticularsTotal { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal Discount { get; private set; }
    public decimal FinalAmount { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public DateTime? AmountReceivedAt { get; private set; }
    public Guid? ReceivedByUserId { get; private set; }
    public Guid? CounterSessionId { get; private set; }

    // Amount actually credited to the counter when this payment was accepted.
    // AcceptVisitFee sets this to VisitFee; AcceptFull sets it to FinalAmount (discount case).
    public decimal CollectedAmount { get; private set; }

    // Amount still owed back to the patient after a post-collection discount. 0 when nothing is due.
    public decimal RefundDue { get; private set; }
    public RefundStatus RefundStatus { get; private set; }
    public DateTime? RefundedAt { get; private set; }
    public Guid? RefundedByUserId { get; private set; }

    private OpdPayment() { }

    public static OpdPayment Create(Guid applicationId, Guid opdRegistrationId, decimal visitFee) => new()
    {
        ApplicationId = applicationId,
        OpdRegistrationId = opdRegistrationId,
        VisitFee = visitFee,
        ParticularsTotal = 0,
        TotalAmount = visitFee,
        Discount = 0,
        FinalAmount = visitFee,
        PaymentStatus = PaymentStatus.Pending,
        CollectedAmount = 0,
        RefundDue = 0,
        RefundStatus = RefundStatus.None,
        CreatedAt = DateTime.UtcNow
    };

    public void UpdateParticularsTotal(decimal particularsTotal)
    {
        ParticularsTotal = particularsTotal;
        TotalAmount = VisitFee + particularsTotal;
        FinalAmount = TotalAmount - Discount;

        // The bill can grow *after* a refund was queued — extra service items added on a
        // discounted visit whose fee was already collected. Re-evaluate what is owed back:
        // if the bill now covers the collected amount the queued refund is no longer due
        // (otherwise Confirm Refund would hand back money the patient still owes).
        if (PaymentStatus == PaymentStatus.Received && RefundStatus == RefundStatus.PendingRefund)
        {
            if (FinalAmount < CollectedAmount)
            {
                RefundDue = CollectedAmount - FinalAmount;
            }
            else
            {
                RefundDue = 0;
                RefundStatus = RefundStatus.None;
            }
        }

        SetUpdatedAt();
    }

    public void ApplyDiscount(decimal discount)
    {
        Discount = discount;
        FinalAmount = TotalAmount - discount;

        // If the fee was already collected, a lower FinalAmount means cash is now owed back to the patient.
        if (PaymentStatus == PaymentStatus.Received && FinalAmount < CollectedAmount)
        {
            RefundDue = CollectedAmount - FinalAmount;
            RefundStatus = RefundStatus.PendingRefund;
        }

        SetUpdatedAt();
    }

    // DEF-OPD-05: the visit itself was cancelled, so the patient receives no service at all —
    // everything collected is now owed back in full. This always re-arms a fresh PendingRefund,
    // even over an earlier RefundStatus.Refunded left by a partial discount while the visit was
    // still active (that cycle settled a smaller, now-irrelevant amount; a cancellation never
    // "already has" a refund for the full collected amount). A no-op when nothing was collected —
    // callers never need to check first.
    public void CancelForFullRefund()
    {
        if (PaymentStatus != PaymentStatus.Received || CollectedAmount <= 0)
            return;

        Discount = TotalAmount;
        FinalAmount = 0;
        RefundDue = CollectedAmount;
        RefundStatus = RefundStatus.PendingRefund;
        SetUpdatedAt();
    }

    // Reception confirms the refund-due amount was physically handed back to the patient.
    public void ProcessRefund(Guid refundedByUserId)
    {
        if (RefundStatus != RefundStatus.PendingRefund)
            throw new InvalidOperationException("No refund is pending for this payment.");

        CollectedAmount -= RefundDue;
        RefundDue = 0;
        RefundStatus = RefundStatus.Refunded;
        RefundedAt = DateTime.UtcNow;
        RefundedByUserId = refundedByUserId;
        SetUpdatedAt();
    }

    // Collect visit fee only. Counter gets VisitFee for this session.
    // Used when services are pending and will be collected individually later.
    public void AcceptVisitFee(Guid receivedByUserId, Guid? counterSessionId)
    {
        if (PaymentStatus == PaymentStatus.Received)
            throw new InvalidOperationException("Visit fee has already been collected.");
        PaymentStatus = PaymentStatus.Received;
        CollectedAmount = VisitFee;
        AmountReceivedAt = DateTime.UtcNow;
        ReceivedByUserId = receivedByUserId;
        CounterSessionId = counterSessionId;
        SetUpdatedAt();
    }

    // Collect the entire bill (visit fee + services - discount) at once.
    // Used when a discount is applied — individual item collection is disabled in that flow.
    // Counter gets FinalAmount; OpdParticulars are marked received via MarkCollectedViaPayment (no individual counter credit).
    // When part of the bill was already collected (a prepaid OPD visit fee, or service items
    // added after the fee was collected), only the outstanding remainder is credited now —
    // the running total still ends up at FinalAmount.
    public void AcceptFull(Guid receivedByUserId, Guid? counterSessionId)
    {
        if (PaymentStatus == PaymentStatus.Received && CollectedAmount >= FinalAmount)
            throw new InvalidOperationException("Payment has already been fully collected.");
        PaymentStatus = PaymentStatus.Received;
        CollectedAmount = FinalAmount;
        AmountReceivedAt = DateTime.UtcNow;
        ReceivedByUserId = receivedByUserId;
        // Keep the session that already credited part of this payment — re-attributing it
        // would silently remove that amount from the earlier session's collected total.
        CounterSessionId ??= counterSessionId;
        SetUpdatedAt();
    }

    // Legacy alias kept for existing callers — behaves as AcceptVisitFee.
    public void Accept(Guid receivedByUserId, Guid? counterSessionId)
        => AcceptVisitFee(receivedByUserId, counterSessionId);

    // DEF-E2E-03: individual service items collected after (or instead of) the visit fee
    // were only ever recorded on the OpdParticular row — CollectedAmount never learned about
    // them, so refund/discount math (which reads CollectedAmount) silently ignored money the
    // patient had actually paid. Folds the item's amount into the payment-level running total.
    // Keeps the earliest CounterSessionId, same rule AcceptFull already uses, so the money stays
    // attributed to whichever session first touched this payment.
    public void RecordItemCollection(decimal amount, Guid receivedByUserId, Guid? counterSessionId)
    {
        CollectedAmount += amount;
        if (PaymentStatus != PaymentStatus.Received)
        {
            PaymentStatus = PaymentStatus.Received;
            AmountReceivedAt = DateTime.UtcNow;
            ReceivedByUserId = receivedByUserId;
        }
        CounterSessionId ??= counterSessionId;
        SetUpdatedAt();
    }

    public void RevertToPending()
    {
        PaymentStatus = PaymentStatus.Pending;
        SetUpdatedAt();
    }
}
