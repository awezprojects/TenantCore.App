using TenantCore.Domain.Common;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

/// <summary>
/// Global subscription plan catalogue — NOT tenant-scoped. Every clinic reads
/// the same four seeded rows (Trial, Monthly, Quarterly, Yearly). Do not add
/// an ApplicationId here; per-clinic purchases live on ClinicSubscription.
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
            CreatedAt = DateTime.UtcNow
        };
}
