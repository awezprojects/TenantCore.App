using TenantCore.Domain.Common;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

/// <summary>
/// One Razorpay payment link and its outcome. Tenant-scoped once a clinic exists —
/// ApplicationId is null for onboarding payments until provisioning, always set for renewals.
/// </summary>
public class SubscriptionPayment : AuditableEntity
{
    public PaymentPurpose Purpose { get; private set; }
    public Guid? OnboardingRequestId { get; private set; }
    public Guid? ApplicationId { get; private set; }

    public Guid SubscriptionPlanId { get; private set; }
    public SubscriptionPlanCode PlanCode { get; private set; }
    public string PlanName { get; private set; } = string.Empty;

    public decimal Amount { get; private set; }
    public long AmountInMinorUnits { get; private set; }
    public string Currency { get; private set; } = string.Empty;

    /// <summary>The plan's list price at creation time — kept alongside Amount so reports can show the discount.</summary>
    public decimal PlanListPrice { get; private set; }

    /// <summary>Links the chain when an admin changes the amount after a link has been created.</summary>
    public Guid? ReplacesPaymentId { get; private set; }
    public Guid? ReplacedByPaymentId { get; private set; }

    public string PayerName { get; private set; } = string.Empty;
    public string PayerEmail { get; private set; } = string.Empty;

    /// <summary>Nullable — a renewing Clinic Admin's phone may genuinely be missing from Auth; Razorpay's customer.contact is optional.</summary>
    public string? PayerPhone { get; private set; }

    public string? GatewayPaymentLinkId { get; private set; }
    public string? PaymentLinkUrl { get; private set; }
    public DateTime? LinkExpiresAt { get; private set; }

    public SubscriptionPaymentStatus Status { get; private set; }
    public string? GatewayPaymentId { get; private set; }
    public string? Method { get; private set; }
    public DateTime? PaidAt { get; private set; }

    public Guid? ClinicSubscriptionId { get; private set; }

    /// <summary>Clinic Admin for a renewal; null for onboarding (the request is admin-initiated).</summary>
    public Guid? InitiatedByUserId { get; private set; }

    private SubscriptionPayment() { }

    public static SubscriptionPayment CreateForOnboarding(
        Guid onboardingRequestId, Guid planId, SubscriptionPlanCode planCode, string planName,
        decimal amount, decimal listPrice, string currency,
        string payerName, string payerEmail, string? payerPhone,
        Guid? replacesPaymentId = null) => new()
        {
            Id = Guid.NewGuid(),
            Purpose = PaymentPurpose.Onboarding,
            OnboardingRequestId = onboardingRequestId,
            SubscriptionPlanId = planId,
            PlanCode = planCode,
            PlanName = planName,
            Amount = amount,
            AmountInMinorUnits = ToMinorUnits(amount),
            Currency = currency,
            PlanListPrice = listPrice,
            PayerName = payerName,
            PayerEmail = payerEmail,
            PayerPhone = payerPhone,
            ReplacesPaymentId = replacesPaymentId,
            Status = SubscriptionPaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

    public static SubscriptionPayment CreateForRenewal(
        Guid applicationId, Guid planId, SubscriptionPlanCode planCode, string planName,
        decimal amount, string currency,
        string payerName, string payerEmail, string? payerPhone, Guid initiatedByUserId) => new()
        {
            Id = Guid.NewGuid(),
            Purpose = PaymentPurpose.Renewal,
            ApplicationId = applicationId,
            SubscriptionPlanId = planId,
            PlanCode = planCode,
            PlanName = planName,
            Amount = amount,
            AmountInMinorUnits = ToMinorUnits(amount),
            Currency = currency,
            PlanListPrice = amount,
            PayerName = payerName,
            PayerEmail = payerEmail,
            PayerPhone = payerPhone,
            InitiatedByUserId = initiatedByUserId,
            Status = SubscriptionPaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

    public static long ToMinorUnits(decimal amount) => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);

    public void SetLink(string gatewayPaymentLinkId, string paymentLinkUrl, DateTime? expiresAt)
    {
        GatewayPaymentLinkId = gatewayPaymentLinkId;
        PaymentLinkUrl = paymentLinkUrl;
        LinkExpiresAt = expiresAt;
        if (Status == SubscriptionPaymentStatus.Pending)
            Status = SubscriptionPaymentStatus.LinkCreated;
        SetUpdatedAt();
    }

    /// <summary>Idempotent — a repeat call with the same GatewayPaymentId is a no-op, so the webhook and reconciliation can both call this safely.</summary>
    public void MarkPaid(string gatewayPaymentId, string? method)
    {
        if (Status == SubscriptionPaymentStatus.Paid && GatewayPaymentId == gatewayPaymentId)
            return;
        if (Status == SubscriptionPaymentStatus.Paid)
            throw new InvalidOperationException("Payment is already marked paid under a different gateway payment id.");

        GatewayPaymentId = gatewayPaymentId;
        Method = method;
        PaidAt = DateTime.UtcNow;
        Status = SubscriptionPaymentStatus.Paid;
        SetUpdatedAt();
    }

    public void AttachClinic(Guid applicationId)
    {
        ApplicationId = applicationId;
        SetUpdatedAt();
    }

    public void AttachSubscription(Guid clinicSubscriptionId)
    {
        ClinicSubscriptionId = clinicSubscriptionId;
        SetUpdatedAt();
    }

    public void MarkExpired()
    {
        if (Status is SubscriptionPaymentStatus.Paid or SubscriptionPaymentStatus.Superseded)
            return;
        Status = SubscriptionPaymentStatus.Expired;
        SetUpdatedAt();
    }

    public void MarkCancelled()
    {
        if (Status is SubscriptionPaymentStatus.Paid or SubscriptionPaymentStatus.Superseded)
            return;
        Status = SubscriptionPaymentStatus.Cancelled;
        SetUpdatedAt();
    }

    public void MarkSuperseded(Guid replacedByPaymentId)
    {
        ReplacedByPaymentId = replacedByPaymentId;
        Status = SubscriptionPaymentStatus.Superseded;
        SetUpdatedAt();
    }

    /// <summary>
    /// Records which payment will replace this one, without changing status yet — used when a
    /// link exists and must be cancelled at Razorpay first; CancelPaymentLinkTaskHandler calls
    /// MarkSuperseded only once that cancellation actually succeeds.
    /// </summary>
    public void SetReplacement(Guid replacedByPaymentId)
    {
        ReplacedByPaymentId = replacedByPaymentId;
        SetUpdatedAt();
    }
}
