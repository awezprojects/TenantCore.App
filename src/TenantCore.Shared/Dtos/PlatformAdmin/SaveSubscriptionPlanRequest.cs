namespace TenantCore.Shared.Dtos.PlatformAdmin;

/// <summary>Body for both create and update. Code is never settable — it is Custom for every admin-created plan.</summary>
public record SaveSubscriptionPlanRequest
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int DurationDays { get; init; }
    public decimal Price { get; init; }
    public bool IsPopular { get; init; }

    /// <summary>False keeps the plan out of every clinic's self-serve list — it is then reachable only through a per-clinic offer.</summary>
    public bool IsPublic { get; init; } = true;

    public int DisplayOrder { get; init; }

    public Guid AdminUserId { get; init; }
    public string AdminEmail { get; init; } = string.Empty;
}
