using TenantCore.Domain.Common;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

public class CounterSession : AuditableEntity
{
    public Guid ApplicationId { get; private set; }
    public DateTime SessionDate { get; private set; }
    public Guid OpenedByUserId { get; private set; }
    public DateTime OpenedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public CounterSessionStatus Status { get; private set; }
    public decimal TotalCollected { get; private set; }
    public decimal TotalExpenses { get; private set; }
    public decimal NetAmount { get; private set; }

    private CounterSession() { }

    public static CounterSession Create(Guid applicationId, Guid openedByUserId, DateTime sessionDate) => new()
    {
        ApplicationId = applicationId,
        OpenedByUserId = openedByUserId,
        SessionDate = sessionDate,
        OpenedAt = DateTime.UtcNow,
        Status = CounterSessionStatus.Open,
        TotalCollected = 0,
        TotalExpenses = 0,
        NetAmount = 0,
        CreatedAt = DateTime.UtcNow
    };

    public void Close(decimal totalCollected, decimal totalExpenses)
    {
        TotalCollected = totalCollected;
        TotalExpenses = totalExpenses;
        NetAmount = totalCollected - totalExpenses;
        Status = CounterSessionStatus.Closed;
        ClosedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }
}
