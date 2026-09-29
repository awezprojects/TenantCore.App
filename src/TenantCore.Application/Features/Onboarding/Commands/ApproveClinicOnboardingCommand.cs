using MediatR;
using TenantCore.Application.Common;

namespace TenantCore.Application.Features.Onboarding.Commands;

/// <summary>Internal — sent by the Admin portal via the InternalService scheme.</summary>
public sealed record ApproveClinicOnboardingCommand(
    Guid Id, Guid? SubscriptionPlanId, bool GrantTrial, decimal? Amount, string? AmountReason,
    string? ClinicCode, string? ReviewNote, Guid AdminUserId, string AdminEmail)
    : IRequest, IBusinessAction
{
    public string ActionName => "Clinic Onboarding Request Approved";
}
