namespace TenantCore.Shared.Dtos;

public class MedicineDosageFormDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record CreateMedicineDosageFormDto(string Name, string? Description);

public sealed record UpdateMedicineDosageFormDto(string Name, string? Description, bool IsActive);
