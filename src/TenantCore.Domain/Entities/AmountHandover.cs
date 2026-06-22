using TenantCore.Domain.Common;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

public class AmountHandover : AuditableEntity
{
    public Guid ApplicationId { get; private set; }
    public Guid CounterSessionId { get; private set; }
    public Guid HandedOverByUserId { get; private set; }
    public Guid HandedOverToUserId { get; private set; }
    public string HandedOverToRoleName { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string? Notes { get; private set; }
    public HandoverStatus Status { get; private set; }
    public DateTime HandedOverAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    public string? ResolutionNotes { get; private set; }

    private AmountHandover() { }

    public static AmountHandover Create(
        Guid applicationId,
        Guid counterSessionId,
        Guid handedOverByUserId,
        Guid handedOverToUserId,
        string handedOverToRoleName,
        decimal amount,
        string? notes) => new()
    {
        ApplicationId = applicationId,
        CounterSessionId = counterSessionId,
        HandedOverByUserId = handedOverByUserId,
        HandedOverToUserId = handedOverToUserId,
        HandedOverToRoleName = handedOverToRoleName,
        Amount = amount,
        Notes = notes,
        Status = HandoverStatus.Pending,
        HandedOverAt = DateTime.UtcNow,
        CreatedAt = DateTime.UtcNow
    };

    public void Accept()
    {
        Status = HandoverStatus.Accepted;
        AcceptedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    public void Dispute(string? resolutionNotes)
    {
        Status = HandoverStatus.Disputed;
        ResolutionNotes = resolutionNotes;
        SetUpdatedAt();
    }
}
