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

    // DEF-MR-12: CreatedDate above is the CLINIC's own creation date. This is the per-user
    // membership date from Auth (UserApplication.CreatedDate) — when THIS user joined THIS
    // clinic — added so a card showing "Joined" can use the member's own date, not the clinic's.
    public DateTime JoinedDate { get; set; }
}
