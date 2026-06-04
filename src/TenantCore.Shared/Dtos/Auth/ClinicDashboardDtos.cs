namespace TenantCore.Shared.Dtos.Auth;

public class CreateClinicRequestDto
{
    public string ClinicName { get; set; } = string.Empty;
    public string ClinicCode { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ContactNumber { get; set; }
    public string? ContactPerson { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? OfficialEmail { get; set; }
    public string? Website { get; set; }
}

public class ClinicDashboardItemDto
{
    public Guid ApplicationId { get; set; }
    public string ClinicName { get; set; } = string.Empty;
    public string ClinicCode { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ContactNumber { get; set; }
    public string? ContactPerson { get; set; }
    public string? OfficialEmail { get; set; }
    public string? Website { get; set; }
    public bool IsOwner { get; set; }
    public List<string> UserRoles { get; set; } = [];
    public DateTime CreatedDate { get; set; }
}
