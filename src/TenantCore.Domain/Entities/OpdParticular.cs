using TenantCore.Domain.Common;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

public class OpdParticular : AuditableEntity
{
    public Guid ApplicationId { get; private set; }
    public Guid OpdRegistrationId { get; private set; }
    public Guid ParticularId { get; private set; }
    public string ParticularName { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }

    // Per-item payment tracking
    public PaymentStatus PaymentStatus { get; private set; }
    public DateTime? CollectedAt { get; private set; }
    public Guid? CollectedByUserId { get; private set; }
    public Guid? CounterSessionId { get; private set; }

    private OpdParticular() { }

    public static OpdParticular Create(Guid applicationId, Guid opdRegistrationId, Guid particularId, string particularName, decimal amount) => new()
    {
        ApplicationId = applicationId,
        OpdRegistrationId = opdRegistrationId,
        ParticularId = particularId,
        ParticularName = particularName,
        Amount = amount,
        PaymentStatus = PaymentStatus.Pending,
        CreatedAt = DateTime.UtcNow
    };

    public void Update(decimal amount)
    {
        Amount = amount;
        SetUpdatedAt();
    }

    // Individual collection — counter credits this item's Amount to the given session.
    public void Collect(Guid collectedByUserId, Guid? counterSessionId)
    {
        if (PaymentStatus == PaymentStatus.Received)
            throw new InvalidOperationException("This service item has already been collected.");
        PaymentStatus = PaymentStatus.Received;
        CollectedAt = DateTime.UtcNow;
        CollectedByUserId = collectedByUserId;
        CounterSessionId = counterSessionId;
        SetUpdatedAt();
    }

    // Used when OpdPayment.AcceptFull() covers this item as part of a discounted total.
    // CounterSessionId is intentionally null — the payment-level CollectedAmount carries the counter credit.
    public void MarkCollectedViaPayment(Guid collectedByUserId)
    {
        PaymentStatus = PaymentStatus.Received;
        CollectedAt = DateTime.UtcNow;
        CollectedByUserId = collectedByUserId;
        CounterSessionId = null;
        SetUpdatedAt();
    }
}
