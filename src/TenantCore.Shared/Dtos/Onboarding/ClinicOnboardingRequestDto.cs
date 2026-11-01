using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos.Onboarding;

/// <summary>
/// User-facing view of a request. Deliberately excludes NeedsAttention/AttentionReason and any
/// admin-only field beyond RejectionReason — those are for the internal admin API only.
/// </summary>
public record ClinicOnboardingRequestDto
{
    public Guid Id { get; init; }
    public string ClinicName { get; init; } = string.Empty;
    public string PreferredClinicCode { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Pincode { get; init; } = string.Empty;
    public string? ClinicContactNumber { get; init; }
    public string? OfficialEmail { get; init; }
    public string? Website { get; init; }
    public string DoctorName { get; init; } = string.Empty;
    public string MedicalRegistrationNumber { get; init; } = string.Empty;
    public string MedicalCouncil { get; init; } = string.Empty;
    public int ExpectedStaffCount { get; init; }
    public string? Notes { get; init; }

    public ClinicOnboardingStatus Status { get; init; }
    public string StatusText { get; init; } = string.Empty;

    public string? ApprovedPlanCode { get; init; }
    public bool IsTrialGrant { get; init; }
    public decimal? ApprovedAmount { get; init; }

    /// <summary>Populated only while Status is AwaitingPayment.</summary>
    public string? PaymentLinkUrl { get; init; }
    public DateTime? LinkExpiresAt { get; init; }

    public Guid? ProvisionedApplicationId { get; init; }
    public string? RejectionReason { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
