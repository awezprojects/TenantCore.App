using TenantCore.Domain.Common;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

/// <summary>
/// One clinic's purchase of a subscription plan — tenant-scoped. A renewal
/// appends a new row rather than overwriting the current one, so history is
/// preserved. Plan details (name, price, duration) are snapshotted at
/// purchase time so a later change to SubscriptionPlan never rewrites history.
/// </summary>
public class ClinicSubscription : AuditableEntity
{
    public Guid ApplicationId { get; private set; }
    public Guid SubscriptionPlanId { get; private set; }
    public SubscriptionPlanCode PlanCode { get; private set; }
    public string PlanName { get; private set; } = string.Empty;
    public decimal PricePaid { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public int DurationDays { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancelledBy { get; private set; }

    // Snapshot of the billing contact at subscribe time. Required because the
    // future Azure Function notification job runs with no user bearer token
    // and cannot ask TenantCore.Auth who to email — see PLAN.md.
    public string ClinicName { get; private set; } = string.Empty;
    public string BillingContactEmail { get; private set; } = string.Empty;
    public string BillingContactName { get; private set; } = string.Empty;

    private ClinicSubscription() { }

    public static ClinicSubscription Create(
        Guid applicationId,
        SubscriptionPlan plan,
        DateTime startDate,
        string clinicName,
        string billingContactEmail,
        string billingContactName) => new()
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            SubscriptionPlanId = plan.Id,
            PlanCode = plan.Code,
            PlanName = plan.Name,
            PricePaid = plan.Price,
            Currency = plan.Currency,
            DurationDays = plan.DurationDays,
            StartDate = startDate,
            EndDate = startDate.AddDays(plan.DurationDays),
            Status = SubscriptionStatus.Active,
            ClinicName = clinicName,
            BillingContactEmail = billingContactEmail,
            BillingContactName = billingContactName,
            CreatedAt = DateTime.UtcNow
        };

    public void Cancel(string cancelledBy)
    {
        Status = SubscriptionStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancelledBy = cancelledBy;
        SetUpdatedAt();
    }

    /// <summary>True when this row currently grants access — status Active and EndDate not yet passed. Evaluated by date, not a background job.</summary>
    public bool IsCurrentlyActive(DateTime utcNow) => Status == SubscriptionStatus.Active && EndDate >= utcNow;
}
