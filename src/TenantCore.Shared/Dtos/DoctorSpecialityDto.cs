namespace TenantCore.Shared.Dtos;

public class DoctorSpecialityDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int SortOrder { get; init; }
}
