namespace TenantCore.Shared.Dtos.PlatformAdmin;

/// <summary>Makes one plan available to one clinic, optionally at a special price.</summary>
public record CreateClinicPlanOfferRequest
{
    public Guid SubscriptionPlanId { get; init; }

    /// <summary>Null means the plan's list price.</summary>
    public decimal? OfferPrice { get; init; }

    /// <summary>Null means the offer never expires.</summary>
    public DateTime? ValidUntil { get; init; }

    public string? Note { get; init; }

    public Guid AdminUserId { get; init; }
    public string AdminEmail { get; init; } = string.Empty;
}
