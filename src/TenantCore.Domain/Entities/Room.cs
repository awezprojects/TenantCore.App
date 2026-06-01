using TenantCore.Domain.Common;

namespace TenantCore.Domain.Entities;

public class Room : AuditableEntity
{
    public Guid ApplicationId { get; private set; }
    public Guid WardId { get; private set; }
    public string RoomNumber { get; private set; } = string.Empty;
    public string? RoomType { get; private set; }
    public decimal PricePerDay { get; private set; }
    public bool IsActive { get; private set; }

    public Ward Ward { get; private set; } = null!;
    public ICollection<Bed> Beds { get; private set; } = [];

    private Room() { }

    public static Room Create(Guid applicationId, Guid wardId, string roomNumber, string? roomType, decimal pricePerDay) => new()
    {
        ApplicationId = applicationId,
        WardId = wardId,
        RoomNumber = roomNumber,
        RoomType = roomType,
        PricePerDay = pricePerDay,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
    };

    public void Update(string roomNumber, string? roomType, decimal pricePerDay)
    {
        RoomNumber = roomNumber;
        RoomType = roomType;
        PricePerDay = pricePerDay;
        SetUpdatedAt();
    }

    public void Deactivate() { IsActive = false; SetUpdatedAt(); }
    public void Activate() { IsActive = true; SetUpdatedAt(); }
}
