namespace TenantCore.Shared.Dtos;

public class ClinicFeatureFlagsDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public bool PrepaidOpdEnabled { get; init; }
    public bool BillingEnabled { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed record UpdateClinicFeatureFlagsDto(bool PrepaidOpdEnabled, bool BillingEnabled);
