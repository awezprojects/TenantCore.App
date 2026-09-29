using MediatR;
using TenantCore.Application.Common;
using TenantCore.Shared.Dtos.Onboarding;

namespace TenantCore.Application.Features.Onboarding.Commands;

/// <summary>UserId/Name/Email come from the caller's JWT claims, never from the request body.</summary>
public sealed record SubmitClinicOnboardingCommand(
    Guid UserId, string RequesterName, string RequesterEmail, SubmitClinicOnboardingRequest Request)
    : IRequest<Guid>, IBusinessAction
{
    public string ActionName => "Clinic Onboarding Request Submitted";
}
