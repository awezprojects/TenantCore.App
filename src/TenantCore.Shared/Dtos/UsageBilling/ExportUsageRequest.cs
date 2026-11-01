namespace TenantCore.Shared.Dtos.UsageBilling;

/// <summary>
/// The client sends back the already-loaded <see cref="ClinicUsageDto"/> so the export endpoint
/// can format it without re-querying the database (workspace rule 7).
/// </summary>
public record ExportUsageRequest
{
    public ClinicUsageDto Data { get; init; } = null!;
    public string Currency { get; init; } = "INR";
}
