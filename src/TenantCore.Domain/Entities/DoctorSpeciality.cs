using TenantCore.Domain.Common;

namespace TenantCore.Domain.Entities;

public class DoctorSpeciality : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }

    public ICollection<DoctorProfile> DoctorProfiles { get; private set; } = [];

    private DoctorSpeciality() { }

    public static DoctorSpeciality Create(string name, string? description = null, int sortOrder = 0) => new()
    {
        Id          = Guid.NewGuid(),
        Name        = name,
        Description = description,
        SortOrder   = sortOrder,
        IsActive    = true,
        CreatedAt   = DateTime.UtcNow,
    };
}
