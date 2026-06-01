namespace TenantCore.Shared.Dtos;

public class BedDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public Guid WardId { get; init; }
    public string WardName { get; init; } = string.Empty;
    public Guid RoomId { get; init; }
    public string RoomNumber { get; init; } = string.Empty;
    public string BedNumber { get; init; } = string.Empty;
    public bool IsOccupied { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record CreateBedDto(Guid RoomId, string BedNumber);
