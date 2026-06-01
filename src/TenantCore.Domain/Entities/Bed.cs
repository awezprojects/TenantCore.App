using TenantCore.Domain.Common;

namespace TenantCore.Domain.Entities;

public class Bed : AuditableEntity
{
    public Guid ApplicationId { get; private set; }
    public Guid WardId { get; private set; }
    public Guid RoomId { get; private set; }
    public string BedNumber { get; private set; } = string.Empty;
    public bool IsOccupied { get; private set; }
    public bool IsActive { get; private set; }

    public Ward Ward { get; private set; } = null!;
    public Room Room { get; private set; } = null!;

    private Bed() { }

    public static Bed Create(Guid applicationId, Guid wardId, Guid roomId, string bedNumber) => new()
    {
        ApplicationId = applicationId,
        WardId = wardId,
        RoomId = roomId,
        BedNumber = bedNumber,
        IsOccupied = false,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
    };

    public void MarkOccupied() { IsOccupied = true; SetUpdatedAt(); }
    public void MarkAvailable() { IsOccupied = false; SetUpdatedAt(); }
    public void Deactivate() { IsActive = false; SetUpdatedAt(); }
    public void Activate() { IsActive = true; SetUpdatedAt(); }
}
