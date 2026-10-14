namespace TenantCore.Shared.Dtos.PlatformAdmin;

/// <summary>
/// Internal — waives the clinic's one-time registration fee. Always reason-bearing: a waived fee is
/// revenue an admin chose not to take, so it is never silent.
/// </summary>
public record WaiveRegistrationFeeRequest
{
    public string Reason { get; init; } = string.Empty;

    public Guid AdminUserId { get; init; }
    public string AdminEmail { get; init; } = string.Empty;
}
