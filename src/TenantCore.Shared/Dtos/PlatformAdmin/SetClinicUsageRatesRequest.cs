namespace TenantCore.Shared.Dtos.PlatformAdmin;

/// <summary>
/// Internal — replaces a clinic's metered prices. One entry per billable operation; an operation left
/// out keeps the price it already has (it is not silently reset to zero).
/// </summary>
public record SetClinicUsageRatesRequest
{
    public IReadOnlyList<ClinicUsageRateEntry> Items { get; init; } = [];

    /// <summary>A negotiated price is a decision, so the portal records why it changed.</summary>
    public string? Reason { get; init; }

    public Guid AdminUserId { get; init; }
    public string AdminEmail { get; init; } = string.Empty;
}

/// <summary>One operation's new price. Zero is allowed and means the operation is free on purpose.</summary>
public record ClinicUsageRateEntry
{
    public string BillingItem { get; init; } = string.Empty;
    public decimal Rate { get; init; }
}
