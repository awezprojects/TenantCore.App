namespace TenantCore.Shared.Dtos.PlatformAdmin;

public record SetClinicPlanVisibilityRequest
{
    /// <summary>True hides the public catalogue from this clinic — it then sees only its own offers.</summary>
    public bool RestrictToOfferedPlans { get; init; }

    public Guid AdminUserId { get; init; }
    public string AdminEmail { get; init; } = string.Empty;
}
