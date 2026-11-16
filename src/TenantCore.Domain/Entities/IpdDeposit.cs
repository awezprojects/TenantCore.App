using TenantCore.Domain.Common;

namespace TenantCore.Domain.Entities;

// Immutable ledger row — any amount, any number of times, any point during an active
// admission (confirmed). No per-deposit refund tracking: settlement happens once, at
// discharge, against the IpdPayment final bill.
public class IpdDeposit : AuditableEntity
{
    public Guid ApplicationId { get; private set; }
    public Guid IpdRegistrationId { get; private set; }
    public decimal Amount { get; private set; }
    public Guid CollectedByUserId { get; private set; }
    public DateTime CollectedAt { get; private set; }
    public Guid? CounterSessionId { get; private set; }
    public string? Notes { get; private set; }

    private IpdDeposit() { }

    public static IpdDeposit Create(
        Guid applicationId,
        Guid ipdRegistrationId,
        decimal amount,
        Guid collectedByUserId,
        Guid? counterSessionId,
        string? notes) => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = applicationId,
        IpdRegistrationId = ipdRegistrationId,
        Amount = amount,
        CollectedByUserId = collectedByUserId,
        CollectedAt = DateTime.UtcNow,
        CounterSessionId = counterSessionId,
        Notes = notes,
        CreatedAt = DateTime.UtcNow
    };
}
