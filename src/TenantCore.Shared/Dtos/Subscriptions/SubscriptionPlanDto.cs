using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos.Subscriptions;

/// <summary>
/// A catalogue entry for the plan picker. SubscriptionPlan is a global,
/// non-tenant-scoped lookup — every clinic sees the same four rows.
/// </summary>
public record SubscriptionPlanDto
{
    public Guid Id { get; init; }
    public SubscriptionPlanCode Code { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int DurationDays { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = string.Empty;
    public bool IsTrial { get; init; }
    public bool IsPopular { get; init; }
    public int DisplayOrder { get; init; }

    /// <summary>True when the current clinic already used its Trial (of any status) — used to grey out the Trial card.</summary>
    public bool AlreadyUsed { get; init; }
}
