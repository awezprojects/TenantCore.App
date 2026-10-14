using MediatR;
using TenantCore.Application.Common;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Onboarding.Commands;

/// <summary>
/// Internal — sent by the Admin portal via the InternalService scheme.
/// All approvals provision directly; no upfront subscription payment.
/// Billing intent (mode, registration fee, free days, PAYG rates) is stored on the request
/// and applied by ActivateSubscriptionTaskHandler after provisioning completes.
/// </summary>
public sealed record ApproveClinicOnboardingCommand(
    Guid Id, bool GrantTrial,
    string? ClinicCode, string? ReviewNote, Guid AdminUserId, string AdminEmail,
    BillingMode? BillingModeOverride = null,
    decimal? RegistrationFeeOverride = null,
    int? FreeTestDaysOverride = null,
    decimal? PayAsYouGoOpdRate = null,
    decimal? PayAsYouGoIpdAdmissionRate = null,
    decimal? PayAsYouGoOtSurgeryRate = null,
    decimal? PayAsYouGoIpdDischargeRate = null,
    decimal? PayAsYouGoInitialWalletCredit = null)
    : IRequest, IBusinessAction
{
    public string ActionName => "Clinic Onboarding Request Approved";

    public bool IsPayAsYouGo => BillingModeOverride == BillingMode.PayAsYouGo;

    public bool HasBillingIntent =>
        BillingModeOverride.HasValue || RegistrationFeeOverride.HasValue || FreeTestDaysOverride.HasValue
        || PayAsYouGoOpdRate.HasValue || PayAsYouGoIpdAdmissionRate.HasValue
        || PayAsYouGoOtSurgeryRate.HasValue || PayAsYouGoIpdDischargeRate.HasValue
        || PayAsYouGoInitialWalletCredit.HasValue;
}
