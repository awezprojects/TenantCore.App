using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos.Subscriptions;

/// <summary>
/// A catalogue entry for the plan picker. SubscriptionPlan itself is a global, non-tenant-scoped
/// lookup, but which rows a clinic sees — and at what price — is per-clinic: a plan is visible
/// when it is public (and the clinic isn't restricted to its own offers) or when the clinic has a
/// live ClinicPlanOffer for it. See IClinicPlanCatalog.
/// </summary>
public record SubscriptionPlanDto
{
    public Guid Id { get; init; }
    public SubscriptionPlanCode Code { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int DurationDays { get; init; }

    /// <summary>What THIS clinic pays — the live offer price when one exists, otherwise ListPrice.</summary>
    public decimal Price { get; init; }

    /// <summary>The plan's catalogue price, shown struck through when an offer beats it.</summary>
    public decimal ListPrice { get; init; }

    /// <summary>True when Price comes from a per-clinic offer rather than the catalogue.</summary>
    public bool IsSpecialOffer { get; init; }

    /// <summary>When the special price stops applying. Null means it does not expire.</summary>
    public DateTime? OfferValidUntil { get; init; }

    public string Currency { get; init; } = string.Empty;
    public bool IsTrial { get; init; }
    public bool IsPopular { get; init; }
    public int DisplayOrder { get; init; }

    /// <summary>True when the current clinic already used its Trial (of any status) — used to grey out the Trial card.</summary>
    public bool AlreadyUsed { get; init; }
}
