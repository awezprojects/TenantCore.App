namespace TenantCore.Shared.Dtos;

public class RoomDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public Guid WardId { get; init; }
    public string WardName { get; init; } = string.Empty;
    public string RoomNumber { get; init; } = string.Empty;
    public string? RoomType { get; init; }
    public decimal PricePerDay { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public int BedCount { get; init; }
    public int AvailableBeds { get; init; }
    public List<BedDto> Beds { get; init; } = [];
}

public sealed record CreateRoomDto(Guid WardId, string RoomNumber, string? RoomType, decimal PricePerDay);
public sealed record UpdateRoomDto(string RoomNumber, string? RoomType, decimal PricePerDay);
