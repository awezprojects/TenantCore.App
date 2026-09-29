using TenantCore.Domain.Common;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

/// <summary>
/// One user's request to have a clinic set up. Not tenant-scoped — no clinic exists yet; scoped
/// to the requesting user. Drives the whole approval → payment → provisioning → activation flow.
/// See plan/clinic-trial-razorpay-subscriptions/PLAN.md for the full state machine.
/// </summary>
public class ClinicOnboardingRequest : AuditableEntity
{
    public Guid RequestedByUserId { get; private set; }
    public string RequesterName { get; private set; } = string.Empty;
    public string RequesterEmail { get; private set; } = string.Empty;
    public string RequesterPhone { get; private set; } = string.Empty;

    public string ClinicName { get; private set; } = string.Empty;
    public string PreferredClinicCode { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string Pincode { get; private set; } = string.Empty;
    public string? ClinicContactNumber { get; private set; }
    public string? OfficialEmail { get; private set; }
    public string? Website { get; private set; }

    public string DoctorName { get; private set; } = string.Empty;
    public string MedicalRegistrationNumber { get; private set; } = string.Empty;
    public string MedicalCouncil { get; private set; } = string.Empty;
    public int ExpectedStaffCount { get; private set; }
    public string? ReferralSource { get; private set; }
    public string? Notes { get; private set; }

    public ClinicOnboardingStatus Status { get; private set; }

    public Guid? ApprovedPlanId { get; private set; }
    public SubscriptionPlanCode? ApprovedPlanCode { get; private set; }
    public decimal? PlanListPrice { get; private set; }
    public decimal? ApprovedAmount { get; private set; }
    public string? AmountReason { get; private set; }
    public bool IsTrialGrant { get; private set; }
    public string? ApprovedClinicCode { get; private set; }

    public Guid? ReviewedByAdminId { get; private set; }
    public string? ReviewedByAdminEmail { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public string? ReviewNote { get; private set; }
    public string? RejectionReason { get; private set; }

    public Guid? CurrentPaymentId { get; private set; }
    public Guid? ProvisionedApplicationId { get; private set; }
    public Guid? ClinicSubscriptionId { get; private set; }
    public DateTime? ActivatedAt { get; private set; }

    /// <summary>Throttles the doctor-facing "check payment" call to once per 30 seconds.</summary>
    public DateTime? LastPaymentCheckAt { get; private set; }

    public bool NeedsAttention { get; private set; }
    public string? AttentionReason { get; private set; }

    private ClinicOnboardingRequest() { }

    public static ClinicOnboardingRequest Submit(
        Guid requestedByUserId, string requesterName, string requesterEmail, string requesterPhone,
        string clinicName, string preferredClinicCode, string address, string city, string state, string pincode,
        string? clinicContactNumber, string? officialEmail, string? website,
        string doctorName, string medicalRegistrationNumber, string medicalCouncil, int expectedStaffCount,
        string? referralSource, string? notes) => new()
        {
            Id = Guid.NewGuid(),
            RequestedByUserId = requestedByUserId,
            RequesterName = requesterName,
            RequesterEmail = requesterEmail,
            RequesterPhone = requesterPhone,
            ClinicName = clinicName,
            PreferredClinicCode = preferredClinicCode.Trim().ToUpperInvariant(),
            Address = address,
            City = city,
            State = state,
            Pincode = pincode,
            ClinicContactNumber = clinicContactNumber,
            OfficialEmail = officialEmail,
            Website = website,
            DoctorName = doctorName,
            MedicalRegistrationNumber = medicalRegistrationNumber,
            MedicalCouncil = medicalCouncil,
            ExpectedStaffCount = expectedStaffCount,
            ReferralSource = referralSource,
            Notes = notes,
            Status = ClinicOnboardingStatus.Submitted,
            CreatedAt = DateTime.UtcNow
        };

    public void ApproveWithPaidPlan(
        Guid planId, SubscriptionPlanCode planCode, decimal listPrice, decimal amount, string? amountReason,
        string? clinicCodeOverride, string? reviewNote, Guid adminId, string adminEmail)
    {
        EnsureStatus(ClinicOnboardingStatus.Submitted, "approve");
        ApprovedPlanId = planId;
        ApprovedPlanCode = planCode;
        PlanListPrice = listPrice;
        ApprovedAmount = amount;
        AmountReason = amountReason;
        IsTrialGrant = false;
        ApplyClinicCodeOverride(clinicCodeOverride);
        ApplyReview(reviewNote, adminId, adminEmail);
        Status = ClinicOnboardingStatus.Approved;
        SetUpdatedAt();
    }

    public void ApproveWithTrial(string? clinicCodeOverride, string? reviewNote, Guid adminId, string adminEmail)
    {
        EnsureStatus(ClinicOnboardingStatus.Submitted, "approve");
        IsTrialGrant = true;
        ApplyClinicCodeOverride(clinicCodeOverride);
        ApplyReview(reviewNote, adminId, adminEmail);
        Status = ClinicOnboardingStatus.Provisioning;
        SetUpdatedAt();
    }

    public void Reject(string reason, Guid adminId, string adminEmail)
    {
        if (Status is not (ClinicOnboardingStatus.Submitted or ClinicOnboardingStatus.Approved or ClinicOnboardingStatus.AwaitingPayment))
            throw new InvalidOperationException($"Cannot reject a request in status {Status}.");
        RejectionReason = reason;
        ApplyReview(null, adminId, adminEmail);
        Status = ClinicOnboardingStatus.Rejected;
        SetUpdatedAt();
    }

    public void Cancel()
    {
        EnsureStatus(ClinicOnboardingStatus.Submitted, "cancel");
        Status = ClinicOnboardingStatus.Cancelled;
        SetUpdatedAt();
    }

    public void SetCurrentPayment(Guid paymentId)
    {
        CurrentPaymentId = paymentId;
        SetUpdatedAt();
    }

    public void MarkAwaitingPayment()
    {
        if (Status is not (ClinicOnboardingStatus.Approved or ClinicOnboardingStatus.AwaitingPayment))
            throw new InvalidOperationException($"Cannot mark awaiting payment from status {Status}.");
        Status = ClinicOnboardingStatus.AwaitingPayment;
        SetUpdatedAt();
    }

    public void MarkPaymentReceived()
    {
        if (Status == ClinicOnboardingStatus.PaymentReceived)
            return; // idempotent — ConfirmPayment may run more than once
        EnsureStatus(ClinicOnboardingStatus.AwaitingPayment, "mark payment received");
        Status = ClinicOnboardingStatus.PaymentReceived;
        SetUpdatedAt();
    }

    public void MarkProvisioning()
    {
        if (Status is not (ClinicOnboardingStatus.PaymentReceived or ClinicOnboardingStatus.Provisioning))
            throw new InvalidOperationException($"Cannot mark provisioning from status {Status}.");
        Status = ClinicOnboardingStatus.Provisioning;
        SetUpdatedAt();
    }

    public void SetProvisionedClinic(Guid applicationId)
    {
        ProvisionedApplicationId = applicationId;
        SetUpdatedAt();
    }

    public void Activate(Guid clinicSubscriptionId)
    {
        ClinicSubscriptionId = clinicSubscriptionId;
        ActivatedAt = DateTime.UtcNow;
        Status = ClinicOnboardingStatus.Active;
        NeedsAttention = false;
        AttentionReason = null;
        SetUpdatedAt();
    }

    public void MarkLinkExpired()
    {
        if (Status is ClinicOnboardingStatus.Active or ClinicOnboardingStatus.Rejected or ClinicOnboardingStatus.Cancelled)
            return; // already terminal in a different, later way — never regress
        Status = ClinicOnboardingStatus.PaymentLinkExpired;
        SetUpdatedAt();
    }

    public void FlagAttention(string reason)
    {
        NeedsAttention = true;
        AttentionReason = Truncate(reason, 2000);
        SetUpdatedAt();
    }

    public void ClearAttention()
    {
        NeedsAttention = false;
        AttentionReason = null;
        SetUpdatedAt();
    }

    public void OverrideClinicCode(string clinicCode)
    {
        ApplyClinicCodeOverride(clinicCode);
        SetUpdatedAt();
    }

    public void UpdateApprovedAmount(decimal amount, string reason)
    {
        if (Status is not (ClinicOnboardingStatus.Approved or ClinicOnboardingStatus.AwaitingPayment))
            throw new InvalidOperationException($"Cannot change the amount from status {Status}.");
        ApprovedAmount = amount;
        AmountReason = reason;
        SetUpdatedAt();
    }

    public void RecordPaymentCheck() => LastPaymentCheckAt = DateTime.UtcNow;

    /// <summary>The admin's override if set, otherwise the requester's preference — what provisioning actually uses.</summary>
    public string EffectiveClinicCode => !string.IsNullOrWhiteSpace(ApprovedClinicCode) ? ApprovedClinicCode! : PreferredClinicCode;

    /// <summary>Non-terminal — the one-open-request-per-user rule checks this.</summary>
    public bool IsOpen => Status is ClinicOnboardingStatus.Submitted or ClinicOnboardingStatus.Approved
        or ClinicOnboardingStatus.AwaitingPayment or ClinicOnboardingStatus.PaymentReceived or ClinicOnboardingStatus.Provisioning;

    private void ApplyClinicCodeOverride(string? clinicCode)
    {
        if (!string.IsNullOrWhiteSpace(clinicCode))
            ApprovedClinicCode = clinicCode.Trim().ToUpperInvariant();
    }

    private void ApplyReview(string? reviewNote, Guid adminId, string adminEmail)
    {
        ReviewNote = reviewNote;
        ReviewedByAdminId = adminId;
        ReviewedByAdminEmail = adminEmail;
        ReviewedAt = DateTime.UtcNow;
    }

    private void EnsureStatus(ClinicOnboardingStatus expected, string action)
    {
        if (Status != expected)
            throw new InvalidOperationException($"Cannot {action} — expected status {expected} but was {Status}.");
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
