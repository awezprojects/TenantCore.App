namespace TenantCore.Domain.Entities;

public class Medicine : TenantCore.Domain.Common.AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? GenericName { get; private set; }
    public string? BrandName { get; private set; }
    public string? Description { get; private set; }
    public string? Composition { get; private set; }
    public string? Composition2 { get; private set; }
    public string? Dosage { get; private set; }
    public string? Form { get; private set; }
    public string? Manufacturer { get; private set; }
    public bool IsGeneric { get; private set; }
    public string? PackSize { get; private set; }
    public string? Uses { get; private set; }
    public string? SideEffects { get; private set; }
    public string? Contraindications { get; private set; }
    public string? Storage { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? MedicineTypeId { get; private set; }

    public MedicineType? MedicineType { get; private set; }

    private Medicine() { }

    public static Medicine Create(
        string name,
        string? genericName,
        string? brandName,
        string? description,
        string? composition,
        string? composition2,
        string? dosage,
        string? form,
        string? manufacturer,
        bool isGeneric,
        string? packSize,
        string? uses,
        string? sideEffects,
        string? contraindications,
        string? storage,
        Guid? medicineTypeId) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        GenericName = genericName,
        BrandName = brandName,
        Description = description,
        Composition = composition,
        Composition2 = composition2,
        Dosage = dosage,
        Form = form,
        Manufacturer = manufacturer,
        IsGeneric = isGeneric,
        PackSize = packSize,
        Uses = uses,
        SideEffects = sideEffects,
        Contraindications = contraindications,
        Storage = storage,
        IsActive = true,
        MedicineTypeId = medicineTypeId,
        CreatedAt = DateTime.UtcNow
    };

    public void Update(
        string name,
        string? genericName,
        string? brandName,
        string? description,
        string? composition,
        string? composition2,
        string? dosage,
        string? form,
        string? manufacturer,
        bool isGeneric,
        string? packSize,
        string? uses,
        string? sideEffects,
        string? contraindications,
        string? storage,
        bool isActive,
        Guid? medicineTypeId)
    {
        Name = name;
        GenericName = genericName;
        BrandName = brandName;
        Description = description;
        Composition = composition;
        Composition2 = composition2;
        Dosage = dosage;
        Form = form;
        Manufacturer = manufacturer;
        IsGeneric = isGeneric;
        PackSize = packSize;
        Uses = uses;
        SideEffects = sideEffects;
        Contraindications = contraindications;
        Storage = storage;
        IsActive = isActive;
        MedicineTypeId = medicineTypeId;
        SetUpdatedAt();
    }
}
