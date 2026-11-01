using TenantCore.Domain.Common;

namespace TenantCore.Domain.Entities;

/// <summary>
/// Makes one plan available to one clinic, optionally at a price only that clinic gets.
/// Tenant-scoped. At most one live offer per (clinic, plan) — enforced by a filtered unique index.
/// </summary>
public class ClinicPlanOffer : AuditableEntity
{
    public Guid ApplicationId { get; private set; }
    public Guid SubscriptionPlanId { get; private set; }

    /// <summary>Null means the clinic pays the plan's list price — the offer then only grants visibility.</summary>
    public decimal? OfferPrice { get; private set; }

    /// <summary>Null means the offer never expires.</summary>
    public DateTime? ValidUntil { get; private set; }

    public string? Note { get; private set; }
    public bool IsActive { get; private set; }

    public string CreatedByAdminEmail { get; private set; } = string.Empty;
    public DateTime? WithdrawnAt { get; private set; }
    public string? WithdrawnByAdminEmail { get; private set; }

    public SubscriptionPlan Plan { get; private set; } = null!;

    private ClinicPlanOffer() { }

    public static ClinicPlanOffer Create(
        Guid applicationId, Guid subscriptionPlanId, decimal? offerPrice,
        DateTime? validUntil, string? note, string adminEmail) => new()
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            SubscriptionPlanId = subscriptionPlanId,
            OfferPrice = offerPrice,
            ValidUntil = validUntil,
            Note = note,
            IsActive = true,
            CreatedByAdminEmail = adminEmail,
            CreatedAt = DateTime.UtcNow
        };

    /// <summary>Active and not past its expiry — the only state in which the offer affects visibility or price.</summary>
    public bool IsLive(DateTime utcNow) => IsActive && (ValidUntil is null || ValidUntil >= utcNow);

    public void Withdraw(string adminEmail)
    {
        if (!IsActive)
            throw new InvalidOperationException("This offer has already been withdrawn.");

        IsActive = false;
        WithdrawnAt = DateTime.UtcNow;
        WithdrawnByAdminEmail = adminEmail;
        SetUpdatedAt();
    }
}
