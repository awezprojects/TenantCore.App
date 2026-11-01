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

    /// <summary>Idempotency guard for activation — at most one subscription per payment (unique filtered index).</summary>
    public Guid? SubscriptionPaymentId { get; private set; }

    /// <summary>Set for a trial grant, which has no payment — guards against a second trial for the same request (unique filtered index).</summary>
    public Guid? OnboardingRequestId { get; private set; }

    /// <summary>The clinic owner (onboarding) or the Clinic Admin who paid (renewal).</summary>
    public Guid? PurchasedByUserId { get; private set; }

    /// <summary>Set when an internal admin activated this term for free — PricePaid is then 0.</summary>
    public string? GrantedByAdminEmail { get; private set; }

    /// <summary>Why the free grant was given. Admin-entered, never shown to the clinic.</summary>
    public string? GrantReason { get; private set; }

    /// <summary>Why an admin cancelled this term before it started.</summary>
    public string? CancellationReason { get; private set; }

    private ClinicSubscription() { }

    public static ClinicSubscription Create(
        Guid applicationId,
        SubscriptionPlan plan,
        DateTime startDate,
        string clinicName,
        string billingContactEmail,
        string billingContactName,
        Guid? purchasedByUserId = null,
        Guid? subscriptionPaymentId = null,
        Guid? onboardingRequestId = null,
        decimal? pricePaidOverride = null) => new()
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            SubscriptionPlanId = plan.Id,
            PlanCode = plan.Code,
            PlanName = plan.Name,
            PricePaid = pricePaidOverride ?? plan.Price,
            Currency = plan.Currency,
            DurationDays = plan.DurationDays,
            StartDate = startDate,
            EndDate = startDate.AddDays(plan.DurationDays),
            Status = SubscriptionStatus.Active,
            ClinicName = clinicName,
            BillingContactEmail = billingContactEmail,
            BillingContactName = billingContactName,
            CreatedAt = DateTime.UtcNow,
            PurchasedByUserId = purchasedByUserId,
            SubscriptionPaymentId = subscriptionPaymentId,
            OnboardingRequestId = onboardingRequestId
        };

    /// <summary>
    /// An internal admin activating a plan for free. PricePaid is 0 regardless of the plan's price,
    /// and the term is queued after existing coverage exactly like a paid one.
    /// </summary>
    public static ClinicSubscription CreateAdminGrant(
        Guid applicationId,
        SubscriptionPlan plan,
        DateTime startDate,
        string clinicName,
        string billingContactEmail,
        string billingContactName,
        string adminEmail,
        string reason)
    {
        var subscription = Create(
            applicationId, plan, startDate, clinicName, billingContactEmail, billingContactName,
            pricePaidOverride: 0m);

        subscription.GrantedByAdminEmail = adminEmail;
        subscription.GrantReason = reason;
        return subscription;
    }

    public void Cancel(string cancelledBy)
    {
        Status = SubscriptionStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancelledBy = cancelledBy;
        SetUpdatedAt();
    }

    /// <summary>
    /// Admin-only cancellation of a term that has not started. A started term is never cancelled —
    /// the clinic has already been using it — so this throws instead.
    /// </summary>
    public void CancelUpcoming(string adminEmail, string reason, DateTime utcNow)
    {
        if (!IsUpcoming(utcNow))
            throw new InvalidOperationException("Only a term that has not started yet can be cancelled.");

        CancellationReason = reason;
        Cancel(adminEmail);
    }

    /// <summary>
    /// Moves an upcoming term earlier (or later) to keep terms contiguous after one is cancelled.
    /// EndDate is recomputed from the snapshotted DurationDays, so the clinic always gets the full
    /// term it paid for. Started terms are never re-dated.
    /// </summary>
    public void Reschedule(DateTime newStartDate, DateTime utcNow)
    {
        if (!IsUpcoming(utcNow))
            throw new InvalidOperationException("Only a term that has not started yet can be rescheduled.");

        StartDate = newStartDate;
        EndDate = newStartDate.AddDays(DurationDays);
        SetUpdatedAt();
    }

    /// <summary>
    /// True when this row grants access right now — Active, already started and not yet ended.
    /// The StartDate check matters: a term bought mid-term is queued for later and must NOT
    /// report as the current plan (nor unlock the clinic) until it actually begins.
    /// </summary>
    public bool IsCurrentlyActive(DateTime utcNow) =>
        Status == SubscriptionStatus.Active && StartDate <= utcNow && EndDate >= utcNow;

    /// <summary>True when this term is paid for (or granted) but has not begun yet.</summary>
    public bool IsUpcoming(DateTime utcNow) => Status == SubscriptionStatus.Active && StartDate > utcNow;
}
