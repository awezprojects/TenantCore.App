using FluentAssertions;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Tests.Entities;

/// <summary>
/// Bill-collection rules for a discounted OPD visit. The regression these cover: when the visit
/// fee had already been collected (prepaid registration) and service items were added afterwards,
/// there was no way to collect the outstanding remainder — AcceptFull refused outright and the
/// UI hid the control — so pending service charges could never be settled.
/// </summary>
public class OpdPaymentTests
{
    private static OpdPayment CreateCollectedVisitFee(decimal visitFee, Guid? sessionId, out Guid applicationId)
    {
        applicationId = Guid.NewGuid();
        var payment = OpdPayment.Create(applicationId, Guid.NewGuid(), visitFee);
        payment.AcceptVisitFee(Guid.NewGuid(), sessionId);
        return payment;
    }

    [Fact]
    public void AcceptFull_WhenNothingCollectedYet_CollectsFinalAmount()
    {
        var applicationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var payment = OpdPayment.Create(applicationId, Guid.NewGuid(), 350);
        payment.UpdateParticularsTotal(350);
        payment.ApplyDiscount(100);

        payment.AcceptFull(Guid.NewGuid(), sessionId);

        payment.PaymentStatus.Should().Be(PaymentStatus.Received);
        payment.CollectedAmount.Should().Be(600);
        payment.CounterSessionId.Should().Be(sessionId);
    }

    [Fact]
    public void AcceptFull_WhenVisitFeeAlreadyCollected_CollectsOnlyTheOutstandingRemainder()
    {
        var sessionId = Guid.NewGuid();
        var payment = CreateCollectedVisitFee(350, sessionId, out _);

        // Two service items were added after the fee was collected, then a discount applied.
        payment.UpdateParticularsTotal(350);
        payment.ApplyDiscount(100);

        payment.CollectedAmount.Should().Be(350);
        payment.FinalAmount.Should().Be(600);

        payment.AcceptFull(Guid.NewGuid(), Guid.NewGuid());

        payment.PaymentStatus.Should().Be(PaymentStatus.Received);
        payment.CollectedAmount.Should().Be(600);
        // The session that already credited the visit fee must keep the payment.
        payment.CounterSessionId.Should().Be(sessionId);
    }

    [Fact]
    public void AcceptFull_WhenAlreadyFullyCollected_Throws()
    {
        var applicationId = Guid.NewGuid();
        var payment = OpdPayment.Create(applicationId, Guid.NewGuid(), 350);
        payment.AcceptFull(Guid.NewGuid(), Guid.NewGuid()); // CollectedAmount -> 350
        payment.CollectedAmount.Should().Be(350);

        var action = () => payment.AcceptFull(Guid.NewGuid(), null);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*already been fully collected*");
    }

    [Fact]
    public void UpdateParticularsTotal_WhenBillGrowsPastQueuedRefund_ClearsTheRefund()
    {
        var payment = CreateCollectedVisitFee(350, Guid.NewGuid(), out _);
        payment.ApplyDiscount(100); // FinalAmount 250 < Collected 350 -> refund due
        payment.RefundStatus.Should().Be(RefundStatus.PendingRefund);
        payment.RefundDue.Should().Be(100);

        // Service items added afterwards push the bill back above what was collected.
        payment.UpdateParticularsTotal(350);

        payment.FinalAmount.Should().Be(600);
        payment.RefundStatus.Should().Be(RefundStatus.None);
        payment.RefundDue.Should().Be(0);
    }

    [Fact]
    public void UpdateParticularsTotal_WhenBillStillBelowCollectedAmount_KeepsRefundDue()
    {
        var payment = CreateCollectedVisitFee(350, Guid.NewGuid(), out _);
        payment.ApplyDiscount(100); // FinalAmount 250 -> RefundDue 100
        payment.UpdateParticularsTotal(0);

        payment.RefundStatus.Should().Be(RefundStatus.PendingRefund);
        payment.RefundDue.Should().Be(100);
    }

    [Fact]
    public void RecordItemCollection_WhenVisitFeeAlreadyCollected_AddsToCollectedAmount()
    {
        var sessionId = Guid.NewGuid();
        var payment = CreateCollectedVisitFee(200, sessionId, out _);
        payment.UpdateParticularsTotal(500);

        payment.RecordItemCollection(500, Guid.NewGuid(), Guid.NewGuid());

        payment.CollectedAmount.Should().Be(700);
        payment.PaymentStatus.Should().Be(PaymentStatus.Received);
        // The session that already credited the visit fee must keep the payment.
        payment.CounterSessionId.Should().Be(sessionId);
    }

    [Fact]
    public void RecordItemCollection_WhenNothingCollectedYet_SetsStatusReceivedAndSession()
    {
        var applicationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var receivedBy = Guid.NewGuid();
        var payment = OpdPayment.Create(applicationId, Guid.NewGuid(), 200);
        payment.UpdateParticularsTotal(300);

        payment.RecordItemCollection(300, receivedBy, sessionId);

        payment.PaymentStatus.Should().Be(PaymentStatus.Received);
        payment.CollectedAmount.Should().Be(300);
        payment.ReceivedByUserId.Should().Be(receivedBy);
        payment.CounterSessionId.Should().Be(sessionId);
    }

    [Fact]
    public void ApplyDiscount_AfterItemsCollectedIndividually_ComputesRefundOverTheFullCollectedAmount()
    {
        // DEF-E2E-03 regression: before the fix, CollectedAmount never learned about individually
        // collected items, so a discount applied afterwards silently ignored that money and
        // under-calculated (or missed) the refund actually owed.
        var sessionId = Guid.NewGuid();
        var payment = CreateCollectedVisitFee(200, sessionId, out _);
        payment.UpdateParticularsTotal(500); // USG 500 added, final amount 700
        payment.RecordItemCollection(500, Guid.NewGuid(), sessionId); // item collected -> CollectedAmount 700

        payment.ApplyDiscount(150); // FinalAmount 550 < CollectedAmount 700 -> refund due

        payment.CollectedAmount.Should().Be(700);
        payment.FinalAmount.Should().Be(550);
        payment.RefundStatus.Should().Be(RefundStatus.PendingRefund);
        payment.RefundDue.Should().Be(150);
    }
}