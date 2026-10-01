using TenantCore.Domain.Common;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

/// <summary>
/// Global subscription plan catalogue — NOT tenant-scoped. It starts as the four seeded rows
/// (Trial, Monthly, Quarterly, Yearly) and internal admins add their own packages on top
/// (Code = Custom). Do not add an ApplicationId here: per-clinic purchases live on
/// ClinicSubscription, and per-clinic visibility/pricing on ClinicPlanOffer.
/// </summary>
public class SubscriptionPlan : AuditableEntity
{
    public SubscriptionPlanCode Code { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int DurationDays { get; private set; }
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public bool IsTrial { get; private set; }
    public bool IsPopular { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>
    /// False keeps the plan out of every clinic's self-serve list — it is then reachable only by a
    /// clinic that has a live ClinicPlanOffer for it, or by an admin assigning it directly.
    /// </summary>
    public bool IsPublic { get; private set; }

    private SubscriptionPlan() { }

    /// <summary>Used only by SubscriptionPlanConfiguration's HasData seed — fixed GUIDs, ValueGeneratedNever.</summary>
    public static SubscriptionPlan CreateForSeed(
        Guid id,
        SubscriptionPlanCode code,
        string name,
        string description,
        int durationDays,
        decimal price,
        string currency,
        bool isTrial,
        bool isPopular,
        int displayOrder) => new()
        {
            Id = id,
            Code = code,
            Name = name,
            Description = description,
            DurationDays = durationDays,
            Price = price,
            Currency = currency,
            IsTrial = isTrial,
            IsPopular = isPopular,
            DisplayOrder = displayOrder,
            IsActive = true,
            IsPublic = true,
            CreatedAt = DateTime.UtcNow
        };

    /// <summary>An admin-created package. Always Code = Custom; the seeded four keep their own codes.</summary>
    public static SubscriptionPlan CreateCustom(
        string name,
        string description,
        int durationDays,
        decimal price,
        bool isPopular,
        int displayOrder,
        bool isPublic) => new()
        {
            Id = Guid.NewGuid(),
            Code = SubscriptionPlanCode.Custom,
            Name = name,
            Description = description,
            DurationDays = durationDays,
            Price = price,
            Currency = "INR",
            IsTrial = false,
            IsPopular = isPopular,
            DisplayOrder = displayOrder,
            IsActive = true,
            IsPublic = isPublic,
            CreatedAt = DateTime.UtcNow
        };

    /// <summary>
    /// Edits the catalogue entry. Existing subscriptions and open payment links are unaffected —
    /// both snapshot name, price and duration at purchase time. Code is immutable.
    /// </summary>
    public void UpdateDetails(
        string name, string description, int durationDays, decimal price,
        bool isPopular, int displayOrder, bool isPublic)
    {
        if (IsTrial && price != 0m)
            throw new InvalidOperationException("The Trial plan must stay free.");

        Name = name;
        Description = description;
        DurationDays = durationDays;
        Price = price;
        IsPopular = isPopular;
        DisplayOrder = displayOrder;
        IsPublic = isPublic;
        SetUpdatedAt();
    }

    public void Activate()
    {
        IsActive = true;
        SetUpdatedAt();
    }

    /// <summary>Hides the plan from clinics, new offers and new links. Existing terms keep running.</summary>
    public void Deactivate()
    {
        IsActive = false;
        SetUpdatedAt();
    }
}
