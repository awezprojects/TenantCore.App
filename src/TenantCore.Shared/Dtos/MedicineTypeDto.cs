namespace TenantCore.Shared.Dtos;

public class MedicineTypeDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record CreateMedicineTypeDto(string Name, string? Description);

public sealed record UpdateMedicineTypeDto(string Name, string? Description, bool IsActive);
