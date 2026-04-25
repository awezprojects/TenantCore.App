namespace TenantCore.Shared.Dtos;

public class DoctorDto
{
    public Guid UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
}
