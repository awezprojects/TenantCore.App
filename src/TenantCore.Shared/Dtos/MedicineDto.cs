namespace TenantCore.Shared.Dtos;

public class MedicineDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? GenericName { get; init; }
    public string? BrandName { get; init; }
    public string? Description { get; init; }
    public string? Composition { get; init; }
    public string? Composition2 { get; init; }
    public string? Dosage { get; init; }
    public string? Form { get; init; }
    public string? Manufacturer { get; init; }
    public bool IsGeneric { get; init; }
    public string? PackSize { get; init; }
    public string? Uses { get; init; }
    public string? SideEffects { get; init; }
    public string? Contraindications { get; init; }
    public string? Storage { get; init; }
    public bool IsActive { get; init; }
    public Guid? MedicineTypeId { get; init; }
    public string? MedicineTypeName { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record CreateMedicineDto(
    string Name,
    string? GenericName,
    string? BrandName,
    string? Description,
    string? Composition,
    string? Composition2,
    string? Dosage,
    string? Form,
    string? Manufacturer,
    bool IsGeneric,
    string? PackSize,
    string? Uses,
    string? SideEffects,
    string? Contraindications,
    string? Storage,
    Guid? MedicineTypeId);

public sealed record UpdateMedicineDto(
    string Name,
    string? GenericName,
    string? BrandName,
    string? Description,
    string? Composition,
    string? Composition2,
    string? Dosage,
    string? Form,
    string? Manufacturer,
    bool IsGeneric,
    string? PackSize,
    string? Uses,
    string? SideEffects,
    string? Contraindications,
    string? Storage,
    bool IsActive,
    Guid? MedicineTypeId);
