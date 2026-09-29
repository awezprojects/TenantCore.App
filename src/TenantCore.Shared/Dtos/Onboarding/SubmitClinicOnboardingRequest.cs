namespace TenantCore.Shared.Dtos.Onboarding;

/// <summary>User-submitted clinic onboarding request body — all fields the requester provides.</summary>
public record SubmitClinicOnboardingRequest
{
    public string ClinicName { get; init; } = string.Empty;
    public string PreferredClinicCode { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Pincode { get; init; } = string.Empty;
    public string? ClinicContactNumber { get; init; }
    public string? OfficialEmail { get; init; }
    public string? Website { get; init; }
    public string RequesterPhone { get; init; } = string.Empty;
    public string DoctorName { get; init; } = string.Empty;
    public string MedicalRegistrationNumber { get; init; } = string.Empty;
    public string MedicalCouncil { get; init; } = string.Empty;
    public int ExpectedStaffCount { get; init; }
    public string? ReferralSource { get; init; }
    public string? Notes { get; init; }
}
