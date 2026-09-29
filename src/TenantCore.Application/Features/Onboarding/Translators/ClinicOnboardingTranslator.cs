using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos.Onboarding;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Onboarding.Translators;

public static class ClinicOnboardingTranslator
{
    /// <summary>currentPayment supplies PaymentLinkUrl/LinkExpiresAt — pass it only when the request is AwaitingPayment.</summary>
    public static ClinicOnboardingRequestDto ToDto(ClinicOnboardingRequest entity, SubscriptionPayment? currentPayment) => new()
    {
        Id = entity.Id,
        ClinicName = entity.ClinicName,
        PreferredClinicCode = entity.PreferredClinicCode,
        Address = entity.Address,
        City = entity.City,
        State = entity.State,
        Pincode = entity.Pincode,
        ClinicContactNumber = entity.ClinicContactNumber,
        OfficialEmail = entity.OfficialEmail,
        Website = entity.Website,
        DoctorName = entity.DoctorName,
        MedicalRegistrationNumber = entity.MedicalRegistrationNumber,
        MedicalCouncil = entity.MedicalCouncil,
        ExpectedStaffCount = entity.ExpectedStaffCount,
        Notes = entity.Notes,
        Status = entity.Status,
        StatusText = ToStatusText(entity.Status),
        ApprovedPlanCode = entity.ApprovedPlanCode?.ToString(),
        IsTrialGrant = entity.IsTrialGrant,
        ApprovedAmount = entity.ApprovedAmount,
        PaymentLinkUrl = entity.Status == ClinicOnboardingStatus.AwaitingPayment ? currentPayment?.PaymentLinkUrl : null,
        LinkExpiresAt = entity.Status == ClinicOnboardingStatus.AwaitingPayment ? currentPayment?.LinkExpiresAt : null,
        ProvisionedApplicationId = entity.ProvisionedApplicationId,
        RejectionReason = entity.RejectionReason,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    public static string ToStatusText(ClinicOnboardingStatus status) => status switch
    {
        ClinicOnboardingStatus.Submitted => "Pending review",
        ClinicOnboardingStatus.Approved => "Approved — preparing payment",
        ClinicOnboardingStatus.AwaitingPayment => "Awaiting payment",
        ClinicOnboardingStatus.PaymentReceived => "Payment received — setting up",
        ClinicOnboardingStatus.Provisioning => "Setting up your clinic",
        ClinicOnboardingStatus.Active => "Active",
        ClinicOnboardingStatus.Rejected => "Not approved",
        ClinicOnboardingStatus.Cancelled => "Cancelled",
        ClinicOnboardingStatus.PaymentLinkExpired => "Payment link expired",
        _ => status.ToString()
    };
}
