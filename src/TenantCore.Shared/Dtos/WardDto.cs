namespace TenantCore.Shared.Dtos;

public class WardDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public int RoomCount { get; init; }
    public int BedCount { get; init; }
    public int AvailableBeds { get; init; }
    public List<RoomDto> Rooms { get; init; } = [];
}

public sealed record CreateWardDto(string Name, string? Description);
public sealed record UpdateWardDto(string Name, string? Description);
