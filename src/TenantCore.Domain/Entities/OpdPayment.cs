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
    public void AcceptFull(Guid receivedByUserId, Guid? counterSessionId)
    {
        if (PaymentStatus == PaymentStatus.Received)
            throw new InvalidOperationException("Payment has already been fully collected.");
        PaymentStatus = PaymentStatus.Received;
        CollectedAmount = FinalAmount;
        AmountReceivedAt = DateTime.UtcNow;
        ReceivedByUserId = receivedByUserId;
        CounterSessionId = counterSessionId;
        SetUpdatedAt();
    }

    // Legacy alias kept for existing callers — behaves as AcceptVisitFee.
    public void Accept(Guid receivedByUserId, Guid? counterSessionId)
        => AcceptVisitFee(receivedByUserId, counterSessionId);

    public void RevertToPending()
    {
        PaymentStatus = PaymentStatus.Pending;
        SetUpdatedAt();
    }
}
