using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos.Onboarding;

/// <summary>
/// Internal — sent by the Admin portal via the InternalService scheme.
/// All approvals provision directly; billing intent is applied post-activation.
/// </summary>
public record ApproveClinicOnboardingRequest
{
    public bool GrantTrial { get; init; }

    /// <summary>Optional override of the requester's preferred clinic code.</summary>
    public string? ClinicCode { get; init; }

    public string? ReviewNote { get; init; }

    public Guid AdminUserId { get; init; }
    public string AdminEmail { get; init; } = string.Empty;

    // Billing intent — all optional; omitting them leaves the clinic at system defaults.
    public BillingMode? BillingModeOverride { get; init; }
    public decimal? RegistrationFeeOverride { get; init; }
    public int? FreeTestDaysOverride { get; init; }
    public decimal? PayAsYouGoOpdRate { get; init; }
    public decimal? PayAsYouGoIpdAdmissionRate { get; init; }
    public decimal? PayAsYouGoOtSurgeryRate { get; init; }
    public decimal? PayAsYouGoIpdDischargeRate { get; init; }
    public decimal? PayAsYouGoInitialWalletCredit { get; init; }
}
