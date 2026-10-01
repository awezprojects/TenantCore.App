namespace TenantCore.Shared.Dtos.PlatformAdmin;

public record SuspendClinicRequest
{
    /// <summary>Shown to every user of the clinic on the suspended screen.</summary>
    public string Message { get; init; } = string.Empty;

    public Guid AdminUserId { get; init; }
    public string AdminEmail { get; init; } = string.Empty;
}
